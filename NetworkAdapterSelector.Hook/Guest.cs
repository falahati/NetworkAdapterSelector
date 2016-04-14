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
        public Guest(RemoteHooking.IContext inContext, string adapterId, string injectionAddress, int injectionDelay,
            bool isDebug)
        {
            _injectionAddress = injectionAddress;
            _injectionDelay = injectionDelay;
            _adapterId = adapterId;
            _isDebug = isDebug;
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

        /// <summary>
        ///     Starts the hooking process
        /// </summary>
        /// <param name="inContext">Should contain information about the environment</param>
        /// <param name="adapterId"><see cref="String" /> identification of the desired network adapter</param>
        /// <param name="injectionAddress">Address of the injection assemply to be used for child processes</param>
        /// <param name="injectionDelay">Number of milisecons after child process creation to try to inject the code</param>
        /// <param name="isDebug">Indicates if injected code should create a log file and print activity informations</param>
        public void Run(RemoteHooking.IContext inContext, string injectionAddress, string adapterId, int injectionDelay,
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
                    (IntPtr lpApplicationName, IntPtr lpCommandLine, IntPtr lpProcessAttributes,
                        IntPtr lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags, IntPtr lpEnvironment,
                        IntPtr lpCurrentDirectory, IntPtr lpStartupInfo, out ProcessInformation lpProcessInformation) =>
                        Do_CreateProcess(lpApplicationName, lpCommandLine, lpProcessAttributes, lpThreadAttributes,
                            bInheritHandles, dwCreationFlags, lpEnvironment,
                            lpCurrentDirectory, lpStartupInfo, out lpProcessInformation, true)));
            AddHook(@"kernel32.dll", "CreateProcessA",
                new Delegates.CreateProcessDelegate(
                    (IntPtr lpApplicationName, IntPtr lpCommandLine, IntPtr lpProcessAttributes,
                        IntPtr lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags, IntPtr lpEnvironment,
                        IntPtr lpCurrentDirectory, IntPtr lpStartupInfo, out ProcessInformation lpProcessInformation) =>
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

        private void AddHook(string libName, string entryPoint, Delegate inNewProc)
        {
            try
            {
                var localHook = LocalHook.Create(LocalHook.GetProcAddress(libName, entryPoint), inNewProc, this);
                // Exclude current thread (EasyHook)
                localHook.ThreadACL.SetExclusiveACL(new[] {0});
                lock (_hooks)
                    _hooks.Add(localHook);
                DebugMessage(entryPoint + "@" + libName + " Hooked Successfully");
            }
            catch (Exception e)
            {
                DebugMessage("Failed to Hook " + entryPoint + "@" + libName);
                DebugMessage(e.ToString());
            }
        }

        private string GenerateCaptionText()
        {
            var networkInterface = GetNetworkInterface();
            var networkAddressV4 = GetNetworkInterfaceIPAddress(AddressFamily.InterNetwork);
            var networkAddressV6 = GetNetworkInterfaceIPAddress(AddressFamily.InterNetworkV6);
            if (networkInterface == null || (networkAddressV4 == null && networkAddressV6 == null))
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

        private IPAddress GetNetworkInterfaceIPAddress(AddressFamily family)
        {
            return GetNetworkInterface()?.GetIPProperties()
                .UnicastAddresses.SingleOrDefault(
                    information => information.Address.AddressFamily == family)?.Address;
        }

        private bool BindSocket(IntPtr socket, AddressFamily addressFamily, IPAddress ipAddress, int portNumber = 0)
        {
            switch (addressFamily)
            {
                case AddressFamily.InterNetwork:
                    if (ipAddress.Equals(IPAddress.Any))
                    {
                        return true;
                    }
                    if (IsIpInRange(ipAddress, IPAddress.Parse("127.0.0.0"), IPAddress.Parse("127.255.255.255")))
                    {
                        return true; // Loopback
                    }
                    if (IsIpInRange(ipAddress, IPAddress.Parse("10.0.0.0"), IPAddress.Parse("10.255.255.255")))
                    {
                        return true; // Private Network
                    }
                    if (IsIpInRange(ipAddress, IPAddress.Parse("172.16.0.0"), IPAddress.Parse("172.31.255.255")))
                    {
                        return true; // Private Network
                    }
                    if (IsIpInRange(ipAddress, IPAddress.Parse("192.168.0.0"), IPAddress.Parse("192.168.255.255")))
                    {
                        return true; // Private Network
                    }
                    if (IsIpInRange(ipAddress, IPAddress.Parse("224.0.0.0"), IPAddress.Parse("239.255.255.255")))
                    {
                        return true; // Multicast
                    }
                    var networkIp = GetNetworkInterfaceIPAddress(addressFamily);
                    if (networkIp == null)
                    {
                        return false;
                    }
                    if (networkIp.Equals(ipAddress))
                    {
                        return true;
                    }
                    var bindIn = new SocketAddressIn
                    {
                        IPAddress = new SocketAddressIn.AddressIn {IpAddress = networkIp},
                        Family = networkIp.AddressFamily,
                        Port = portNumber
                    };
                    var addressIn = bindIn;
                    ContinueExecution(() => DebugMessage("Auto Bind: " + addressIn.IPAddress.IpAddress));
                    return Socket.Bind(socket, ref bindIn, Marshal.SizeOf(bindIn)) == SocketError.Success;
                default:
                    return true;
            }
        }

        private bool Do_CreateProcess(IntPtr lpApplicationName, IntPtr lpCommandLine, IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags, IntPtr lpEnvironment,
            IntPtr lpCurrentDirectory, IntPtr lpStartupInfo, out ProcessInformation lpProcessInformation, bool unicode)
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

        private SocketError Do_Bind(IntPtr socket, ref SocketAddressIn address, int addressSize)
        {
            var addressIn = address;
            ContinueExecution(
                () => DebugMessage("Bind: " + addressIn.IPAddress.IpAddress + ":" + addressIn.Port));

            var networkIp = GetNetworkInterfaceIPAddress(address.Family);
            if (networkIp != null && !address.IPAddress.IpAddress.Equals(IPAddress.Any) &&
                !address.IPAddress.IpAddress.Equals(networkIp) &&
                !IsIpInRange(address.IPAddress.IpAddress, IPAddress.Parse("127.0.0.0"),
                    IPAddress.Parse("127.255.255.255")))
            {
                address.IPAddress.IpAddress = networkIp;
                ContinueExecution(() => DebugMessage("Modified Bind: " + addressIn.IPAddress.IpAddress));
            }
            return Socket.Bind(socket, ref address, addressSize);
        }


        private SocketError Do_WsaConnect(IntPtr socket, ref SocketAddressIn address, int addressSize, IntPtr inBuffer,
            IntPtr outBuffer, IntPtr sQos, IntPtr gQos)
        {
            var addressIn = address;
            ContinueExecution(() => DebugMessage("WsaConnect: " + addressIn.IPAddress.IpAddress));

            if (!BindSocket(socket, address.Family, address.IPAddress.IpAddress))
            {
                DebugMessage(Socket.WSAGetLastError().ToString());
                return SocketError.SocketError;
            }

            var returnValue = Socket.WSAConnect(socket, ref address, addressSize, inBuffer, outBuffer, sQos, gQos);
            //if (returnValue == SocketError.SocketError
            //    && (Socket.WSAGetLastError() == SocketError.WouldBlock
            //        || Socket.WSAGetLastError() == SocketError.Success))
            //{
            //    // Non blocking mode
            //    returnValue = SocketError.Success;
            //}

            if (returnValue == SocketError.SocketError && Socket.WSAGetLastError() == SocketError.Success)
            {
                returnValue = SocketError.Success;
            }

            return returnValue;
        }

        private SocketError Do_Connect(IntPtr socket, ref SocketAddressIn address, int addressSize)
        {
            var addressIn = address;
            ContinueExecution(() => DebugMessage("Connect: " + addressIn.IPAddress.IpAddress));

            if (!BindSocket(socket, address.Family, address.IPAddress.IpAddress))
            {
                DebugMessage(Socket.WSAGetLastError().ToString());
                return SocketError.SocketError;
            }

            var returnValue = Socket.Connect(socket, ref address, addressSize);
            //if (returnValue == SocketError.SocketError
            //    && (Socket.WSAGetLastError() == SocketError.WouldBlock
            //        || Socket.WSAGetLastError() == SocketError.Success))
            //{
            //    // Non blocking mode
            //    returnValue = SocketError.Success;
            //}

            if (returnValue == SocketError.SocketError && Socket.WSAGetLastError() == SocketError.Success)
            {
                returnValue = SocketError.Success;
            }

            return returnValue;
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
                if ((lowerBoundary && addressBytes[i] < lowerBytes[i]) ||
                    (upperBoundary && addressBytes[i] > upperBytes[i]))
                {
                    return false;
                }

                lowerBoundary &= (addressBytes[i] == lowerBytes[i]);
                upperBoundary &= (addressBytes[i] == upperBytes[i]);
            }

            return true;
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
    }
}