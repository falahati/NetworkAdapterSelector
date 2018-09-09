using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using EasyHook;
using NetworkAdapterSelector.Hook.Unmanaged;
using Process = System.Diagnostics.Process;
using Socket = NetworkAdapterSelector.Hook.Unmanaged.Socket;
using SocketAddress = NetworkAdapterSelector.Hook.Unmanaged.SocketAddress;

namespace NetworkAdapterSelector.Hook
{
    /// <summary>
    ///     A class containing the code to be injected into the application
    /// </summary>
    public class Guest : IEntryPoint
    {
        private readonly string _adapterId;

        private readonly List<LocalHook> _hooks = new List<LocalHook>();

        private readonly string _injectionAddress;
        private readonly int _injectionDelay;

        private readonly bool _isDebug;

        private IntPtr _activeWindow = IntPtr.Zero;


        // ReSharper disable once UnusedParameter.Local
        /// <summary>
        ///     Initializing the Guest class
        /// </summary>
        /// <param name="inContext">Should contain information about the environment</param>
        /// <param name="adapterId"><see cref="String" /> identification of the desired network adapter</param>
        /// <param name="injectionAddress">Address of the injection assemply to be used for child processes</param>
        /// <param name="injectionDelay">Number of milisecons after child process creation to try to inject the code</param>
        /// <param name="isDebug">Indicates if injected code should create a log file and print activity informations</param>
        public Guest(
            RemoteHooking.IContext inContext,
            string adapterId,
            string injectionAddress,
            int injectionDelay,
            bool isDebug)
        {
            _injectionAddress = injectionAddress;
            _injectionDelay = injectionDelay;
            _adapterId = adapterId;
            _isDebug = isDebug;
        }

        private static bool IsIpInRange(IPAddress address, IPAddress lowerRange, IPAddress upperRange)
        {
            var lowerBytes = lowerRange.GetAddressBytes();
            var upperBytes = upperRange.GetAddressBytes();
            var addressBytes = address.GetAddressBytes();
            var lowerBoundary = true;
            var upperBoundary = true;

            for (var i = 0;
                i < lowerBytes.Length &&
                (lowerBoundary || upperBoundary);
                i++)
            {
                if (lowerBoundary && addressBytes[i] < lowerBytes[i] ||
                    upperBoundary && addressBytes[i] > upperBytes[i])
                {
                    return false;
                }

                lowerBoundary &= addressBytes[i] == lowerBytes[i];
                upperBoundary &= addressBytes[i] == upperBytes[i];
            }

            return true;
        }

        /// <summary>
        ///     Starts the hooking process
        /// </summary>
        /// <param name="inContext">Should contain information about the environment</param>
        /// <param name="adapterId"><see cref="String" /> identification of the desired network adapter</param>
        /// <param name="injectionAddress">Address of the injection assemply to be used for child processes</param>
        /// <param name="injectionDelay">Number of milisecons after child process creation to try to inject the code</param>
        /// <param name="isDebug">Indicates if injected code should create a log file and print activity informations</param>
        public void Run(
            RemoteHooking.IContext inContext,
            string injectionAddress,
            string adapterId,
            int injectionDelay,
            bool isDebug)
        {
            LoadLibrary(@"ws2_32.dll", () =>
            {
                AddHook(@"ws2_32.dll", "connect", new Delegates.ConnectDelegate(Do_Connect));
                AddHook(@"ws2_32.dll", "WSAConnect", new Delegates.WsaConnectDelegate(Do_WsaConnect));
                AddHook(@"ws2_32.dll", "bind", new Delegates.BindDelegate(Do_Bind));
            });

            AddHook(@"kernel32.dll", "CreateProcessW",
                new Delegates.CreateProcessDelegate(
                    (
                            IntPtr lpApplicationName,
                            IntPtr lpCommandLine,
                            IntPtr lpProcessAttributes,
                            IntPtr lpThreadAttributes,
                            bool bInheritHandles,
                            uint dwCreationFlags,
                            IntPtr lpEnvironment,
                            IntPtr lpCurrentDirectory,
                            IntPtr lpStartupInfo,
                            out ProcessInformation lpProcessInformation) =>
                            Do_CreateProcess(lpApplicationName, lpCommandLine, lpProcessAttributes, lpThreadAttributes,
                                bInheritHandles, dwCreationFlags, lpEnvironment,
                                lpCurrentDirectory, lpStartupInfo, out lpProcessInformation, true)));
            AddHook(@"kernel32.dll", "CreateProcessA",
                new Delegates.CreateProcessDelegate(
                    (
                            IntPtr lpApplicationName,
                            IntPtr lpCommandLine,
                            IntPtr lpProcessAttributes,
                            IntPtr lpThreadAttributes,
                            bool bInheritHandles,
                            uint dwCreationFlags,
                            IntPtr lpEnvironment,
                            IntPtr lpCurrentDirectory,
                            IntPtr lpStartupInfo,
                            out ProcessInformation lpProcessInformation) =>
                            Do_CreateProcess(lpApplicationName, lpCommandLine, lpProcessAttributes, lpThreadAttributes,
                                bInheritHandles, dwCreationFlags, lpEnvironment,
                                lpCurrentDirectory, lpStartupInfo, out lpProcessInformation, false)));

            // Ansi version of the SetWindowText method
            AddHook(@"user32.dll", "SetWindowTextA",
                new Delegates.SetWindowTextDelegate((handle, text) => Do_SetWindowText(handle, text, false)));

            // Unicode (Wide) version of the SetWindowText method
            AddHook(@"user32.dll", "SetWindowTextW",
                new Delegates.SetWindowTextDelegate((handle, text) => Do_SetWindowText(handle, text, true)));

            // In case we started the application using CreateAndInject method
            RemoteHooking.WakeUpProcess();

            while (true)
            {
                // Return if we failed to hook any method
                lock (_hooks)
                {
                    if (!_hooks.Any())
                    {
                        return;
                    }
                }

                try
                {
                    // Get the host process
                    var currentProcess = Process.GetCurrentProcess();
                    // We do care only about the main window
                    var mainWindowHandler = currentProcess.MainWindowHandle;

                    if (mainWindowHandler != IntPtr.Zero && !string.IsNullOrEmpty(currentProcess.MainWindowTitle))
                    {
                        if (_activeWindow != mainWindowHandler)
                        {
                            // In case main window changed, we need to clean the older one
                            ContinueExecution(() => WindowTitle.CleanWindowsTitle(_activeWindow));
                            _activeWindow = mainWindowHandler;
                        }

                        // Making sure that our special text is in the title bar
                        WindowTitle.AppendWindowTitle(_activeWindow, GenerateCaptionText());
                    }

                    Thread.Sleep(300);
                }
                catch (Exception e)
                {
                    // Ignoring the InvalidOperationException as it happens a lot when program don't have 
                    // a valid window
                    if (!(e is InvalidOperationException))
                    {
                        DebugMessage(e.ToString());
                    }

                    Thread.Sleep(1000);
                }
            }
        }

        private void AddHook(string libName, string entryPoint, Delegate inNewProc)
        {
            try
            {
                var localHook = LocalHook.Create(LocalHook.GetProcAddress(libName, entryPoint), inNewProc, null);
                // Exclude current thread (EasyHook)
                localHook.ThreadACL.SetExclusiveACL(new[] {0});

                lock (_hooks)
                {
                    _hooks.Add(localHook);
                }

                DebugMessage(entryPoint + "@" + libName + " Hooked Successfully");
            }
            catch (Exception e)
            {
                DebugMessage("Failed to Hook " + entryPoint + "@" + libName);
                DebugMessage(e.ToString());
            }
        }

        private SocketError BindSocket(IntPtr socket, ref ISocketAddress address)
        {
            switch (address.Family)
            {
                case AddressFamily.InterNetwork:

                    if (address.Address.IPAddress.Equals(IPAddress.Any))
                    {
                        return SocketError.Success;
                    }

                    if (IsIpInRange(address.Address.IPAddress, IPAddress.Parse("127.0.0.0"),
                        IPAddress.Parse("127.255.255.255")))
                    {
                        return SocketError.Success; // Loopback
                    }

                    if (IsIpInRange(address.Address.IPAddress, IPAddress.Parse("10.0.0.0"),
                        IPAddress.Parse("10.255.255.255")))
                    {
                        return SocketError.Success; // Private Network
                    }

                    if (IsIpInRange(address.Address.IPAddress, IPAddress.Parse("172.16.0.0"),
                        IPAddress.Parse("172.31.255.255")))
                    {
                        return SocketError.Success; // Private Network
                    }

                    if (IsIpInRange(address.Address.IPAddress, IPAddress.Parse("192.168.0.0"),
                        IPAddress.Parse("192.168.255.255")))
                    {
                        return SocketError.Success; // Private Network
                    }

                    if (IsIpInRange(address.Address.IPAddress, IPAddress.Parse("169.254.1.0"),
                        IPAddress.Parse("169.254.254.255")))
                    {
                        return SocketError.Success; // Link Local Network
                    }

                    if (IsIpInRange(address.Address.IPAddress, IPAddress.Parse("224.0.0.0"),
                        IPAddress.Parse("239.255.255.255")))
                    {
                        return SocketError.Success; // Multicast
                    }

                    break;
                case AddressFamily.InterNetworkV6:

                    if (address.Address.IPAddress.Equals(IPAddress.IPv6Any))
                    {
                        return SocketError.Success;
                    }

                    if (address.Address.IPAddress.Equals(IPAddress.IPv6Loopback))
                    {
                        return SocketError.Success; // Loopback
                    }

                    if (IsIpInRange(address.Address.IPAddress,
                        IPAddress.Parse("fc00:0000:0000:0000:0000:0000:0000:0000"),
                        IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")))
                    {
                        return SocketError.Success; // Unique Local Addresses, Private networks, Multicast
                    }

                    //if (((address as SocketAddressIn6?)?.ScopeId ?? 0) > 0)
                    //{
                    //    return true; // Includes explicit interface?
                    //}
                    break;
                default:

                    return SocketError.Success;
            }

            var networkIPAddress = GetNetworkInterfaceIPAddress(address.Family);

            if (networkIPAddress == null)
            {
                return SocketError.SocketError;
            }

            if (networkIPAddress.Equals(address.Address.IPAddress))
            {
                return SocketError.Success;
            }

            switch (networkIPAddress.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    var bindIn = new SocketAddressIn
                    {
                        Address = new AddressIn {IPAddress = networkIPAddress},
                        Family = networkIPAddress.AddressFamily,
                        Port = address.Port
                    };

                    var tmpAddress = address;
                    ContinueExecution(() => DebugMessage("Bind: " + tmpAddress.Address.IPAddress));
                    address = bindIn;

                    return Socket.Bind(socket, ref bindIn, Marshal.SizeOf(bindIn));
                case AddressFamily.InterNetworkV6:
                    var bindIn6 = new SocketAddressIn6
                    {
                        Address = new AddressIn6 {IPAddress = networkIPAddress},
                        Family = networkIPAddress.AddressFamily,
                        Port = address.Port,
                        ScopeId = (address as SocketAddressIn6?)?.ScopeId ?? 0,
                        FlowInfo = (address as SocketAddressIn6?)?.FlowInfo ?? 0
                    };

                    var tmpAddress6 = address;
                    ContinueExecution(() => DebugMessage("Bind: " + tmpAddress6.Address.IPAddress));
                    address = bindIn6;

                    return Socket.Bind(socket, ref bindIn6, Marshal.SizeOf(bindIn6));
                default:

                    return SocketError.Success;
            }
        }

        private void ContinueExecution(Action code)
        {
            try
            {
                code();
            }
            catch (Exception e)
            {
                DebugMessage(e.ToString());
            }
        }


        private void DebugMessage(string p)
        {
            try
            {
                if (!_isDebug)
                {
                    return;
                }

                var process = Process.GetCurrentProcess();
                File.AppendAllText(
                    Path.Combine(Path.GetTempPath(),
                        "NetworkAdapterSelector-" + process.ProcessName + "[" + process.Id + "].log"),
                    string.Format("{0}{1}{2}{1}", new string('-', 30), Environment.NewLine, p));
            }
            catch
            {
                // ignored
            }
        }

        private SocketError Do_Bind(IntPtr socket, IntPtr address, int addressSize)
        {
            var socketAddress = GetSocketAddress(address);

            var temAddress = socketAddress;
            ContinueExecution(
                () => DebugMessage("Bind: " + temAddress.Address.IPAddress + ":" + temAddress.Port));

            return BindSocket(socket, ref socketAddress);
        }

        private SocketError Do_Connect(IntPtr socket, IntPtr address, int addressSize)
        {
            var socketAddress = GetSocketAddress(address);

            var tmpAddress = socketAddress;
            ContinueExecution(
                () => DebugMessage("Connect: " + tmpAddress.Address.IPAddress + ":" + tmpAddress.Port));

            var bindResult = BindSocket(socket, ref socketAddress);

            if (bindResult != SocketError.Success)
            {
                if (bindResult != SocketError.SocketError)
                {
                    DebugMessage(Socket.WSAGetLastError().ToString());
                }

                return SocketError.SocketError;
            }

            var returnValue = SocketError.SocketError;

            if (socketAddress.Family == AddressFamily.InterNetwork)
            {
                var tmpAddress4 = (SocketAddressIn) socketAddress;
                returnValue = Socket.Connect(socket, ref tmpAddress4, addressSize);
            }
            else if (socketAddress.Family == AddressFamily.InterNetworkV6)
            {
                var tmpAddress6 = (SocketAddressIn6) socketAddress;
                returnValue = Socket.Connect(socket, ref tmpAddress6, addressSize);
            }

            if (returnValue == SocketError.SocketError && Socket.WSAGetLastError() == SocketError.Success)
            {
                returnValue = SocketError.Success;
            }

            return returnValue;
        }

        private bool Do_CreateProcess(
            IntPtr lpApplicationName,
            IntPtr lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            IntPtr lpCurrentDirectory,
            IntPtr lpStartupInfo,
            out ProcessInformation lpProcessInformation,
            bool unicode)
        {
            var res = unicode
                ? Unmanaged.Process.CreateProcessW(lpApplicationName, lpCommandLine, lpProcessAttributes,
                    lpThreadAttributes,
                    bInheritHandles, dwCreationFlags, lpEnvironment, lpCurrentDirectory, lpStartupInfo,
                    out lpProcessInformation)
                : Unmanaged.Process.CreateProcessA(lpApplicationName, lpCommandLine, lpProcessAttributes,
                    lpThreadAttributes,
                    bInheritHandles, dwCreationFlags, lpEnvironment, lpCurrentDirectory, lpStartupInfo,
                    out lpProcessInformation);

            var processId = lpProcessInformation.dwProcessId;
            DebugMessage("CreateProcess: #" + processId);

            new Thread(() =>
            {
                Thread.Sleep(_injectionDelay);
                ContinueExecution(
                    () =>
                    {
                        RemoteHooking.Inject(processId, _injectionAddress, _injectionAddress, _adapterId,
                            _injectionAddress, _injectionDelay, _isDebug);
                    });
            }).Start();

            return res;
        }

        private bool Do_SetWindowText(IntPtr windowHandle, IntPtr text, bool unicode)
        {
            var title = unicode ? Marshal.PtrToStringUni(text) : Marshal.PtrToStringAnsi(text);

            if (!string.IsNullOrEmpty(title) && windowHandle.Equals(_activeWindow))
            {
                DebugMessage("SetWindowText: " + windowHandle + " - " + title);
                title = WindowTitle.AppendWindowTitle(title, GenerateCaptionText());
            }

            return Window.SetWindowText(windowHandle, title);
        }


        private SocketError Do_WsaConnect(
            IntPtr socket,
            IntPtr address,
            int addressSize,
            IntPtr inBuffer,
            IntPtr outBuffer,
            IntPtr sQos,
            IntPtr gQos)
        {
            var socketAddress = GetSocketAddress(address);

            var tmpAddress = socketAddress;
            ContinueExecution(
                () => DebugMessage("WsaConnect: " + tmpAddress.Address.IPAddress + ":" + tmpAddress.Port));

            var bindResult = BindSocket(socket, ref socketAddress);

            if (bindResult != SocketError.Success)
            {
                if (bindResult != SocketError.SocketError)
                {
                    DebugMessage(Socket.WSAGetLastError().ToString());
                }

                return SocketError.SocketError;
            }

            var returnValue = SocketError.SocketError;

            if (socketAddress.Family == AddressFamily.InterNetwork)
            {
                var tmpAddress4 = (SocketAddressIn) socketAddress;
                returnValue = Socket.WSAConnect(socket, ref tmpAddress4, addressSize, inBuffer, outBuffer, sQos, gQos);
            }
            else if (socketAddress.Family == AddressFamily.InterNetworkV6)
            {
                var tmpAddress6 = (SocketAddressIn6) socketAddress;
                returnValue = Socket.WSAConnect(socket, ref tmpAddress6, addressSize, inBuffer, outBuffer, sQos, gQos);
            }

            if (returnValue == SocketError.SocketError && Socket.WSAGetLastError() == SocketError.Success)
            {
                returnValue = SocketError.Success;
            }

            return returnValue;
        }

        private string GenerateCaptionText()
        {
            var networkInterface = GetNetworkInterface();
            var networkAddressV4 = GetNetworkInterfaceIPAddress(AddressFamily.InterNetwork);
            var networkAddressV6 = GetNetworkInterfaceIPAddress(AddressFamily.InterNetworkV6);

            if (networkInterface == null || networkAddressV4 == null && networkAddressV6 == null)
            {
                return null;
            }

            return "[" + networkInterface.Name + " - " + (networkAddressV4 ?? networkAddressV6) + "]";
        }

        private NetworkInterface GetNetworkInterface()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .SingleOrDefault(
                    @interface =>
                        @interface.Id.Equals(_adapterId, StringComparison.CurrentCultureIgnoreCase) &&
                        @interface.OperationalStatus == OperationalStatus.Up);
        }

        private IPAddress GetNetworkInterfaceIPAddress(AddressFamily preferredFamily)
        {
            var addresses = GetNetworkInterface()?.GetIPProperties()
                .UnicastAddresses;

            return addresses?.SingleOrDefault(
                       information => information.Address.AddressFamily == preferredFamily)?.Address ??
                   addresses?.SingleOrDefault(information =>
                       information.Address.AddressFamily == AddressFamily.InterNetwork ||
                       information.Address.AddressFamily == AddressFamily.InterNetworkV6)?.Address;
        }

        private ISocketAddress GetSocketAddress(IntPtr socketAddressPointer)
        {
            var type = GetSocketAddressType(socketAddressPointer);

            if (type == null)
            {
                return null;
            }

            return (ISocketAddress) Marshal.PtrToStructure(socketAddressPointer, type);
        }

        private Type GetSocketAddressType(IntPtr socketAddressPointer)
        {
            var socketAddress = (SocketAddress) Marshal.PtrToStructure(socketAddressPointer, typeof(SocketAddress));

            switch (socketAddress.Family)
            {
                case AddressFamily.InterNetwork:
                {
                    return typeof(SocketAddressIn);
                }
                case AddressFamily.InterNetworkV6:
                {
                    return typeof(SocketAddressIn6);
                }
                default:

                    return null;
            }
        }

        private void LoadLibrary(string libraryName, Action code)
        {
            // Forcing the hook by pre-loading the desired library
            var library = Library.LoadLibrary(libraryName);
            code();

            // Unload the library if only we were the ones loading it for the first time
            if (!library.Equals(IntPtr.Zero))
            {
                Library.FreeLibrary(library);
            }
        }

        /// <summary>
        ///     Removes all active hooks
        /// </summary>
        ~Guest()
        {
            ContinueExecution(() =>
            {
                lock (_hooks)
                {
                    foreach (var localHook in _hooks)
                    {
                        ContinueExecution(() => localHook.Dispose());
                    }

                    _hooks.Clear();
                }
            });
        }
    }
}