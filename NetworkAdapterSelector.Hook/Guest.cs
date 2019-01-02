using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using EasyHook;
using NetworkAdapterSelector.Hook.UnManaged;
using Process = System.Diagnostics.Process;
using Socket = NetworkAdapterSelector.Hook.UnManaged.Socket;
using SocketAddress = NetworkAdapterSelector.Hook.UnManaged.SocketAddress;

namespace NetworkAdapterSelector.Hook
{
    /// <summary>
    ///     A class containing the code to be injected into the application
    /// </summary>
    public sealed class Guest : IEntryPoint
    {
        // ReSharper disable once UnusedParameter.Local
        /// <summary>
        ///     Initializing the Guest class
        /// </summary>
        /// <param name="inContext">Should contain information about the environment</param>
        /// <param name="adapterId"><see cref="string" /> identification of the desired network adapter</param>
        /// <param name="injectionGuessAddress">Address of the injection assembly to be used for child processes</param>
        /// <param name="injectionDelay">Number of milliseconds after child process creation to try to inject the code</param>
        /// <param name="isInDebug">Indicates if injected code should create a log file and print activity information</param>
        // ReSharper disable once TooManyDependencies
        public Guest(
            RemoteHooking.IContext inContext,
            string adapterId,
            string injectionGuessAddress,
            int injectionDelay,
            bool isInDebug)
        {
            DebugMessage(
                nameof(Guest),
                "Initializing ..."
            );
            InjectionGuessAddress = injectionGuessAddress;
            InjectionDelay = injectionDelay;
            AdapterId = adapterId;

            if (isInDebug)
            {
                try
                {
                    var process = Process.GetCurrentProcess();
                    LogPath = Path.Combine(Path.GetTempPath(),
                        "NetworkAdapterSelector-" + process.ProcessName + "[" + process.Id + "].log");
                }
                catch
                {
                    // ignored
                }
            }
        }

        private IntPtr ActiveWindow { get; set; } = IntPtr.Zero;
        private string AdapterId { get; }
        private List<LocalHook> Hooks { get; } = new List<LocalHook>();
        private int InjectionDelay { get; }
        private string InjectionGuessAddress { get; }
        private string LogPath { get; }

        /// <summary>
        ///     Starts the hooking process
        /// </summary>
        /// <param name="inContext">Should contain information about the environment</param>
        /// <param name="adapterId"><see cref="String" /> identification of the desired network adapter</param>
        /// <param name="injectionAddress">Address of the injection assembly to be used for child processes</param>
        /// <param name="injectionDelay">Number of milliseconds after child process creation to try to inject the code</param>
        /// <param name="isDebug">Indicates if injected code should create a log file and print activity information</param>
        // ReSharper disable once TooManyArguments
        // ReSharper disable once MethodTooLong
        // ReSharper disable once MethodNameNotMeaningful
        public void Run(
            RemoteHooking.IContext inContext,
            string injectionAddress,
            string adapterId,
            int injectionDelay,
            bool isDebug)
        {
            DebugMessage(
                nameof(Run),
                "Starting ..."
            );

            LoadLibrary(@"ws2_32.dll", () =>
            {
                AddHook(@"ws2_32.dll", "connect", new Delegates.ConnectDelegate(OnConnect));
                AddHook(@"ws2_32.dll", "WSAConnect", new Delegates.WSAConnectDelegate(OnWSAConnect));
                AddHook(@"ws2_32.dll", "bind", new Delegates.BindDelegate(OnBind));
            });

            AddHook(@"kernel32.dll", "CreateProcessA",
                new Delegates.CreateProcessDelegate(
                    (
                        IntPtr applicationName,
                        IntPtr commandLine,
                        IntPtr processAttributes,
                        IntPtr threadAttributes,
                        bool inheritHandles,
                        uint creationFlags,
                        IntPtr environment,
                        IntPtr currentDirectory,
                        IntPtr startupInfo,
                        out ProcessInformation processInformation
                    ) => OnCreateProcess(
                        applicationName,
                        commandLine,
                        processAttributes,
                        threadAttributes,
                        inheritHandles,
                        creationFlags,
                        environment,
                        currentDirectory,
                        startupInfo,
                        out processInformation,
                        false
                    )
                )
            );

            AddHook(@"kernel32.dll", "CreateProcessW",
                new Delegates.CreateProcessDelegate(
                    (
                        IntPtr applicationName,
                        IntPtr commandLine,
                        IntPtr processAttributes,
                        IntPtr threadAttributes,
                        bool inheritHandles,
                        uint creationFlags,
                        IntPtr environment,
                        IntPtr currentDirectory,
                        IntPtr startupInfo,
                        out ProcessInformation processInformation
                    ) => OnCreateProcess(
                        applicationName,
                        commandLine,
                        processAttributes,
                        threadAttributes,
                        inheritHandles,
                        creationFlags,
                        environment,
                        currentDirectory,
                        startupInfo,
                        out processInformation,
                        true
                    )
                )
            );

            // Ansi version of the SetWindowText method
            AddHook(@"user32.dll", "SetWindowTextA",
                new Delegates.SetWindowTextDelegate(
                    (handle, text) => OnSetWindowText(handle, text, false)
                )
            );

            // Unicode (Wide) version of the SetWindowText method
            AddHook(@"user32.dll", "SetWindowTextW",
                new Delegates.SetWindowTextDelegate(
                    (handle, text) => OnSetWindowText(handle, text, true)
                )
            );

            // Return if we failed to hook any method
            lock (Hooks)
            {
                if (!Hooks.Any())
                {
                    DebugMessage(
                        nameof(Run),
                        "FATAL: Failed to hook any function."
                    );

                    return;
                }
            }

            // In case we started the application using CreateAndInject method
            RemoteHooking.WakeUpProcess();

            // Going into a loop to update the application's main window's title bar
            WindowTitleCheckLoop();
        }

        private void AddHook(string libName, string entryPoint, Delegate newProcedure)
        {
            try
            {
                var localHook = LocalHook.Create(LocalHook.GetProcAddress(libName, entryPoint), newProcedure, null);

                // Exclude current thread (EasyHook)
                localHook.ThreadACL.SetExclusiveACL(new[] {0});

                lock (Hooks)
                {
                    Hooks.Add(localHook);
                }

                DebugMessage(
                    nameof(AddHook),
                    "`{0}` @ `{1}` hooked successfully.",
                    entryPoint,
                    libName
                );
            }
            catch (Exception e)
            {
                DebugMessage(nameof(AddHook), e.ToString());
                DebugMessage(
                    nameof(AddHook),
                    "Failed to hook `{0}` @ `{1}`.",
                    entryPoint,
                    libName
                );
            }
        }

        private SocketError BindSocketByAddress(IntPtr socket, ISocketAddress socketAddress)
        {
            switch (socketAddress?.AddressFamily)
            {
                case AddressFamily.InterNetwork:

                    if (socketAddress.Address?.IPAddress?.Equals(IPAddress.Any) == true)
                    {
                        DebugMessage(
                            nameof(BindSocketByAddress),
                            "Binding to interface skipped due to the nature of passed IP Address. [0.0.0.0]"
                        );

                        return SocketError.Success;
                    }

                    if (IsIpInRange(socketAddress.Address?.IPAddress, IPAddress.Parse("127.0.0.0"),
                        IPAddress.Parse("127.255.255.255")))
                    {
                        DebugMessage(
                            nameof(BindSocketByAddress),
                            "Binding to interface skipped due to the nature of passed IP Address. [Loop Back]"
                        );

                        return SocketError.Success; // LoopBack
                    }

                    if (IsIpInRange(socketAddress.Address?.IPAddress, IPAddress.Parse("10.0.0.0"),
                        IPAddress.Parse("10.255.255.255")))
                    {
                        DebugMessage(
                            nameof(BindSocketByAddress),
                            "Binding to interface skipped due to the nature of passed IP Address. [Private Range]"
                        );

                        return SocketError.Success; // Private Network
                    }

                    if (IsIpInRange(socketAddress.Address?.IPAddress, IPAddress.Parse("172.16.0.0"),
                        IPAddress.Parse("172.31.255.255")))
                    {
                        DebugMessage(
                            nameof(BindSocketByAddress),
                            "Binding to interface skipped due to the nature of passed IP Address. [Private Range]"
                        );

                        return SocketError.Success; // Private Network
                    }

                    if (IsIpInRange(socketAddress.Address?.IPAddress, IPAddress.Parse("192.168.0.0"),
                        IPAddress.Parse("192.168.255.255")))
                    {
                        DebugMessage(
                            nameof(BindSocketByAddress),
                            "Binding to interface skipped due to the nature of passed IP Address. [Private Range]"
                        );

                        return SocketError.Success; // Private Network
                    }

                    if (IsIpInRange(socketAddress.Address?.IPAddress, IPAddress.Parse("169.254.1.0"),
                        IPAddress.Parse("169.254.254.255")))
                    {
                        DebugMessage(
                            nameof(BindSocketByAddress),
                            "Binding to interface skipped due to the nature of passed IP Address. [Link Local Network]"
                        );

                        return SocketError.Success; // Link Local Network
                    }

                    if (IsIpInRange(socketAddress.Address?.IPAddress, IPAddress.Parse("224.0.0.0"),
                        IPAddress.Parse("239.255.255.255")))
                    {
                        DebugMessage(
                            nameof(BindSocketByAddress),
                            "Binding to interface skipped due to the nature of passed IP Address. [MultiCast Address]"
                        );

                        return SocketError.Success; // MultiCast
                    }

                    break;
                case AddressFamily.InterNetworkV6:

                    if (socketAddress.Address?.IPAddress?.Equals(IPAddress.IPv6Any) == true)
                    {
                        DebugMessage(
                            nameof(BindSocketByAddress),
                            "Binding to interface skipped due to the nature of passed IP Address. [0000:]"
                        );

                        return SocketError.Success;
                    }

                    if (socketAddress.Address?.IPAddress?.Equals(IPAddress.IPv6Loopback) == true)
                    {
                        DebugMessage(
                            nameof(BindSocketByAddress),
                            "Binding to interface skipped due to the nature of passed IP Address. [Loop Back]"
                        );

                        return SocketError.Success; // LoopBack
                    }

                    if (IsIpInRange(socketAddress.Address?.IPAddress,
                        IPAddress.Parse("fc00:0000:0000:0000:0000:0000:0000:0000"),
                        IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")))
                    {
                        DebugMessage(
                            nameof(BindSocketByAddress),
                            "Binding to interface skipped due to the nature of passed IP Address. [fc00:]"
                        );

                        return SocketError.Success; // Unique Local Addresses, Private networks, MultiCast
                    }

                    break;
                default:
                    DebugMessage(
                        nameof(BindSocketByAddress),
                        "Binding to interface skipped due an unsupported address family of `{0}`.",
                        socketAddress?.AddressFamily
                    );

                    return SocketError.Success;
            }

            var networkInterface = GetNetworkInterface();
            var interfaceAddress = GetInterfaceAddress(networkInterface, socketAddress.AddressFamily, false);

            if (networkInterface == null || interfaceAddress == null)
            {
                DebugMessage(
                    nameof(OnBind),
                    "Binding for the `{0}:{1}` rejected due to lack of a valid interface address.",
                    socketAddress.Address?.IPAddress,
                    socketAddress.Port
                );

                return SocketError.SocketError;
            }

            if (interfaceAddress.AddressFamily == AddressFamily.InterNetwork)
            {
                var bindIn = new SocketAddressIn
                {
                    Address = new AddressIn {IPAddress = interfaceAddress},
                    AddressFamily = interfaceAddress.AddressFamily,
                    Port = 0 // Assign a random port
                };

                DebugMessage(
                    nameof(BindSocketByAddress),
                    "Binding to `{0}:{1}` ...",
                    bindIn.Address?.IPAddress,
                    bindIn.Port
                );

                return Socket.Bind(socket, ref bindIn, Marshal.SizeOf(bindIn));
            }

            if (interfaceAddress.AddressFamily == AddressFamily.InterNetworkV6)
            {
                var scopeId = (uint?) networkInterface.GetIPProperties()?.GetIPv6Properties()?.Index ?? 0;
                var flowInfo = (socketAddress as SocketAddressIn6?)?.FlowInfo ?? 0;

                var bindIn6 = new SocketAddressIn6
                {
                    Address = new AddressIn6 {IPAddress = interfaceAddress},
                    AddressFamily = interfaceAddress.AddressFamily,
                    Port = 0, // Assign a random port
                    ScopeId = scopeId,
                    FlowInfo = flowInfo
                };

                DebugMessage(
                    nameof(BindSocketByAddress),
                    "Binding to `{0}:{1}` ...",
                    bindIn6.Address?.IPAddress,
                    bindIn6.Port
                );

                return Socket.Bind(socket, ref bindIn6, Marshal.SizeOf(bindIn6));
            }

            DebugMessage(
                nameof(BindSocketByAddress),
                "Binding to interface skipped due an unsupported interface address family of `{0}`.",
                interfaceAddress.AddressFamily
            );

            return SocketError.Success;
        }

        private void DebugMessage(string scope, string message, params object[] args)
        {
            var lastError = Socket.WSAGetLastError();

            try
            {
                if (string.IsNullOrWhiteSpace(LogPath))
                {
                    return;
                }

                var space = Math.Min(15 - scope.Length, 0);

                message = string.Format(
                    "{0:s} - [`{1}`] : {2}{3}",
                    DateTime.UtcNow,
                    scope,
                    new string(' ', space),
                    args?.Length > 0 ? string.Format(message, args) : message
                );

                Debug.WriteLine(message);

                File.AppendAllText(LogPath, message + Environment.NewLine);
            }
            catch
            {
                // ignored
            }

            Socket.WSASetLastError(lastError);
        }

        private string GenerateCaptionText()
        {
            var networkInterface = GetNetworkInterface();
            var interfaceAddress = GetInterfaceAddress(networkInterface, AddressFamily.InterNetwork, true);

            if (networkInterface == null || interfaceAddress == null)
            {
                return null;
            }

            return "[" + networkInterface.Name + " - " + interfaceAddress + "]";
        }

        // ReSharper disable once FlagArgument
        private IPAddress GetInterfaceAddress(
            NetworkInterface @interface,
            AddressFamily? preferredFamily,
            bool fallback)
        {
            var result = preferredFamily == null
                ? null
                : @interface
                    ?.GetIPProperties()
                    ?.UnicastAddresses
                    ?.FirstOrDefault(information => information.Address.AddressFamily == preferredFamily.Value)
                    ?.Address;

            if (result == null && fallback)
            {
                return @interface
                    ?.GetIPProperties()
                    ?.UnicastAddresses
                    ?.FirstOrDefault(information =>
                        information.Address.AddressFamily == AddressFamily.InterNetwork ||
                        information.Address.AddressFamily == AddressFamily.InterNetworkV6)
                    ?.Address;
            }

            return result;
        }

        private NetworkInterface GetNetworkInterface()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .SingleOrDefault(
                    @interface =>
                        @interface.Id.Equals(AdapterId, StringComparison.CurrentCultureIgnoreCase) &&
                        @interface.OperationalStatus == OperationalStatus.Up &&
                        @interface.SupportsMulticast);
        }

        private ISocketAddress GetSocketAddress(IntPtr socketAddressPointer)
        {
            if (socketAddressPointer == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                var socketAddress = (SocketAddress) Marshal.PtrToStructure(socketAddressPointer, typeof(SocketAddress));

                Type type;

                switch (socketAddress.Family)
                {
                    case AddressFamily.InterNetwork:
                    {
                        type = typeof(SocketAddressIn);

                        break;
                    }
                    case AddressFamily.InterNetworkV6:
                    {
                        type = typeof(SocketAddressIn6);

                        break;
                    }
                    default:

                        return null;
                }

                return (ISocketAddress) Marshal.PtrToStructure(socketAddressPointer, type);
            }
            catch
            {
                return null;
            }
        }

        private bool IsIpInRange(IPAddress address, IPAddress lowerRange, IPAddress upperRange)
        {
            if (address == null)
            {
                return false;
            }

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

        private void LoadLibrary(string libraryName, Action code)
        {
            // Forcing the hook by pre-loading the desired library
            var library = Library.LoadLibrary(libraryName);
            code();

            // Unload the library only if we were the one loading it for the first time
            if (!library.Equals(IntPtr.Zero))
            {
                Library.FreeLibrary(library);
                DebugMessage(
                    nameof(LoadLibrary),
                    "Library `{1}` loaded and unloaded to override hooked function addresses.",
                    libraryName
                );
            }
        }

        private SocketError OnBind(IntPtr socket, IntPtr address, int addressSize)
        {
            var socketAddress = GetSocketAddress(address);

            DebugMessage(
                nameof(OnBind),
                "Binding to `{0}:{1}` ...",
                socketAddress?.Address?.IPAddress,
                socketAddress?.Port
            );

            var networkInterface = GetNetworkInterface();
            var interfaceAddress = GetInterfaceAddress(networkInterface, socketAddress?.AddressFamily, true);

            if (socketAddress?.Address == null || networkInterface == null || interfaceAddress == null)
            {
                DebugMessage(
                    nameof(OnBind),
                    "Binding to `{0}:{1}` rejected due to lack of a valid interface address.",
                    socketAddress?.Address?.IPAddress,
                    socketAddress?.Port
                );

                return SocketError.SocketError;
            }

            SocketError bindResult;

            if (interfaceAddress.AddressFamily == AddressFamily.InterNetwork &&
                !interfaceAddress.Equals(socketAddress.Address.IPAddress))
            {
                var bindIn = new SocketAddressIn
                {
                    Address = new AddressIn {IPAddress = interfaceAddress},
                    AddressFamily = interfaceAddress.AddressFamily,
                    Port = socketAddress.Port
                };

                DebugMessage(
                    nameof(OnBind),
                    "Binding to `{0}:{1}` replaced by a binding request to `{2}:{3}`.",
                    socketAddress.Address.IPAddress,
                    socketAddress.Port,
                    bindIn.Address.IPAddress,
                    bindIn.Port
                );

                socketAddress = bindIn;

                bindResult = Socket.Bind(socket, ref bindIn, Marshal.SizeOf(bindIn));
            }
            else if (interfaceAddress.AddressFamily == AddressFamily.InterNetworkV6 &&
                     !interfaceAddress.Equals(socketAddress.Address.IPAddress))
            {
                var scopeId = (uint?) networkInterface.GetIPProperties()?.GetIPv6Properties()?.Index ?? 0;
                var flowInfo = (socketAddress as SocketAddressIn6?)?.FlowInfo ?? 0;

                var bindIn6 = new SocketAddressIn6
                {
                    Address = new AddressIn6 {IPAddress = interfaceAddress},
                    AddressFamily = interfaceAddress.AddressFamily,
                    Port = socketAddress.Port, // Assign a random port
                    ScopeId = scopeId,
                    FlowInfo = flowInfo
                };

                DebugMessage(
                    nameof(OnBind),
                    "Binding to `{0}:{1}` replaced by a binding request to `{2}:{3}`.",
                    socketAddress.Address.IPAddress,
                    socketAddress.Port,
                    bindIn6.Address.IPAddress,
                    bindIn6.Port
                );

                socketAddress = bindIn6;

                bindResult = Socket.Bind(socket, ref bindIn6, Marshal.SizeOf(bindIn6));
            }
            else
            {
                bindResult = Socket.Bind(socket, address, addressSize);
            }

            DebugMessage(
                nameof(OnBind),
                "Binding to `{0}:{1}` resulted in a `{2}` response. [WSALastError = `{3}`]",
                socketAddress.Address.IPAddress,
                socketAddress.Port,
                bindResult,
                Socket.WSAGetLastError()
            );

            return bindResult;
        }

        private SocketError OnConnect(IntPtr socket, IntPtr address, int addressSize)
        {
            var socketAddress = GetSocketAddress(address);

            DebugMessage(
                nameof(OnConnect),
                "Connecting to `{0}:{1}` ...",
                socketAddress?.Address?.IPAddress,
                socketAddress?.Port
            );

            var bindResult = BindSocketByAddress(socket, socketAddress);

            DebugMessage(
                nameof(OnConnect),
                "Binding `{0}:{1}` to interface resulted in a `{2}` response. [WSALastError = `{3}`]",
                socketAddress?.Address?.IPAddress,
                socketAddress?.Port,
                bindResult,
                Socket.WSAGetLastError()
            );

            if (bindResult != SocketError.Success)
            {
                if (bindResult != SocketError.SocketError)
                {
                    DebugMessage(nameof(OnConnect), Socket.WSAGetLastError().ToString());
                }

                DebugMessage(
                    nameof(OnConnect),
                    "Connecting to `{0}:{1}` rejected.",
                    socketAddress?.Address?.IPAddress,
                    socketAddress?.Port
                );

                return SocketError.SocketError;
            }

            var returnValue = Socket.Connect(socket, address, addressSize);

            DebugMessage(
                nameof(OnConnect),
                "Connecting to `{0}:{1}` resulted in a `{2}` response. [WSALastError = `{3}`]",
                socketAddress?.Address?.IPAddress,
                socketAddress?.Port,
                returnValue,
                Socket.WSAGetLastError()
            );

            //if (returnValue == SocketError.SocketError && Socket.WSAGetLastError() == SocketError.Success)
            //{
            //    returnValue = SocketError.Success;
            //}

            return returnValue;
        }

        // ReSharper disable once TooManyArguments
        private bool OnCreateProcess(
            IntPtr applicationName,
            IntPtr commandLine,
            IntPtr processAttributes,
            IntPtr threadAttributes,
            bool inheritHandles,
            uint creationFlags,
            IntPtr environment,
            IntPtr currentDirectory,
            IntPtr startupInfo,
            out ProcessInformation processInformation,
            bool isUnicode)
        {
            var resultValue = isUnicode
                ? UnManaged.Process.CreateProcessW(
                    applicationName,
                    commandLine,
                    processAttributes,
                    threadAttributes,
                    inheritHandles,
                    creationFlags,
                    environment,
                    currentDirectory,
                    startupInfo,
                    out processInformation
                )
                : UnManaged.Process.CreateProcessA(
                    applicationName,
                    commandLine,
                    processAttributes,
                    threadAttributes,
                    inheritHandles,
                    creationFlags,
                    environment,
                    currentDirectory,
                    startupInfo,
                    out processInformation
                );

            if (!resultValue)
            {
                return false;
            }

            if (processInformation.ProcessId <= 0)
            {
                return true;
            }

            var processId = processInformation.ProcessId;

            DebugMessage(nameof(OnCreateProcess), "A new process with identification number of #{0} is created.",
                processId);

            new Thread(() =>
            {
                Thread.Sleep(InjectionDelay);
                var tries = 1;

                while (true)
                {
                    try
                    {
                        RemoteHooking.Inject(
                            processId,
                            InjectionGuessAddress,
                            InjectionGuessAddress,
                            AdapterId,
                            InjectionGuessAddress,
                            InjectionDelay,
                            !string.IsNullOrWhiteSpace(LogPath)
                        );

                        DebugMessage(nameof(OnCreateProcess), "Process #{0} injected with the guest code.", processId);

                        return;
                    }
                    catch
                    {
                        if (tries < 3)
                        {
                            tries++;

                            Thread.Sleep(1000);

                            continue;
                        }

                        DebugMessage(nameof(OnCreateProcess), "Failed to inject the guest code to process #{0}.",
                            processId);

                        return;
                    }
                }
            }).Start();

            return true;
        }

        private bool OnSetWindowText(IntPtr windowHandle, IntPtr text, bool unicode)
        {
            var title = unicode ? Marshal.PtrToStringUni(text) : Marshal.PtrToStringAnsi(text);

            if (!string.IsNullOrEmpty(title) && windowHandle.Equals(ActiveWindow))
            {
                DebugMessage(nameof(OnSetWindowText), "Window #{0} title changing to `{1}`.", windowHandle, title);
                title = WindowTitle.AppendWindowTitle(title, GenerateCaptionText());
            }

            return Window.SetWindowText(windowHandle, title);
        }


        // ReSharper disable once TooManyArguments
        private SocketError OnWSAConnect(
            IntPtr socket,
            IntPtr address,
            int addressSize,
            IntPtr inBuffer,
            IntPtr outBuffer,
            IntPtr sQos,
            IntPtr gQos)
        {
            var socketAddress = GetSocketAddress(address);

            DebugMessage(
                nameof(OnWSAConnect),
                "Connecting to `{0}:{1}` ...",
                socketAddress?.Address?.IPAddress,
                socketAddress?.Port
            );

            var bindResult = BindSocketByAddress(socket, socketAddress);

            DebugMessage(
                nameof(OnWSAConnect),
                "Binding `{0}:{1}` to interface resulted in a `{2}` response. [WSALastError = `{3}`]",
                socketAddress?.Address?.IPAddress,
                socketAddress?.Port,
                bindResult,
                Socket.WSAGetLastError()
            );

            if (bindResult != SocketError.Success)
            {
                if (bindResult != SocketError.SocketError)
                {
                    DebugMessage(nameof(OnWSAConnect), Socket.WSAGetLastError().ToString());
                }

                DebugMessage(
                    nameof(OnWSAConnect),
                    "Connecting to `{0}:{1}` rejected.",
                    socketAddress?.Address?.IPAddress,
                    socketAddress?.Port
                );

                return SocketError.SocketError;
            }

            var returnValue = Socket.WSAConnect(socket, address, addressSize, inBuffer, outBuffer, sQos, gQos);

            DebugMessage(
                nameof(OnWSAConnect),
                "Connecting to `{0}:{1}` resulted in a `{2}` response. [WSALastError = `{3}`]",
                socketAddress?.Address?.IPAddress,
                socketAddress?.Port,
                returnValue,
                Socket.WSAGetLastError()
            );

            //if (returnValue == SocketError.SocketError && Socket.WSAGetLastError() == SocketError.Success)
            //{
            //    returnValue = SocketError.Success;
            //}

            return returnValue;
        }

        private void WindowTitleCheckLoop()
        {
            while (true)
            {
                try
                {
                    // Get the host process
                    var currentProcess = Process.GetCurrentProcess();

                    // We do care only about the main window
                    var mainWindowHandler = currentProcess.MainWindowHandle;

                    if (mainWindowHandler != IntPtr.Zero &&
                        !string.IsNullOrEmpty(currentProcess.MainWindowTitle))
                    {
                        if (ActiveWindow != mainWindowHandler)
                        {
                            if (ActiveWindow != IntPtr.Zero)
                            {
                                DebugMessage(
                                    nameof(WindowTitleCheckLoop),
                                    "Main window changed from #{0} to #{1}. Cleaning old window's title bar.",
                                    ActiveWindow,
                                    mainWindowHandler
                                );

                                // In case main window changed, we need to clean the older one
                                try
                                {
                                    WindowTitle.CleanWindowsTitle(ActiveWindow);
                                }
                                catch
                                {
                                    // ignored
                                }
                            }

                            ActiveWindow = mainWindowHandler;
                        }

                        // Making sure that our special text is in the title bar
                        WindowTitle.AppendWindowTitle(ActiveWindow, GenerateCaptionText());
                    }

                    Thread.Sleep(300);
                }
                catch (Exception e)
                {
                    // Ignoring the InvalidOperationException as it happens a lot when program don't have 
                    // a valid window
                    if (!(e is InvalidOperationException))
                    {
                        DebugMessage(nameof(WindowTitleCheckLoop), e.ToString());
                    }

                    Thread.Sleep(1000);
                }
            }

            // ReSharper disable once FunctionNeverReturns
        }

        /// <summary>
        ///     Removes all active hooks
        /// </summary>
        ~Guest()
        {
            try
            {
                lock (Hooks)
                {
                    foreach (var localHook in Hooks)
                    {
                        try
                        {
                            localHook.Dispose();
                        }
                        catch
                        {
                            // ignored
                        }
                    }

                    Hooks.Clear();
                }
            }
            catch
            {
                // ignored
            }
        }
    }
}