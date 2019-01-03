using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using NetworkAdapterSelector.Hook.UnManaged.Structures;

namespace NetworkAdapterSelector.Hook
{
    internal static class Delegates
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        public delegate SocketError BindDelegate(IntPtr socket, IntPtr address, int addressSize);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        public delegate SocketError ConnectDelegate(IntPtr socket, IntPtr address, int addressSize);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        public delegate bool CreateProcessDelegate(
            IntPtr applicationName,
            IntPtr commandLine,
            IntPtr processAttributes,
            IntPtr threadAttributes,
            bool inheritHandles,
            uint creationFlags,
            IntPtr environment,
            IntPtr currentDirectory,
            IntPtr startupInfo,
            out ProcessInformation processInformation);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        public delegate bool SetWindowTextDelegate(IntPtr windowHandle, IntPtr textPointer);
        
        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        public delegate SocketError CloseDelegate(IntPtr socket);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        public delegate IntPtr OpenDelegate(AddressFamily addressFamily, SocketType type, ProtocolType protocol);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        public delegate IntPtr WSAOpenDelegate(
            AddressFamily addressFamily,
            SocketType type,
            ProtocolType protocol,
            IntPtr protocolInfo,
            int groupId,
            short flags);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        public delegate SocketError WSAConnectDelegate(
            IntPtr socket, IntPtr address, int addressSize, IntPtr inBuffer, IntPtr outBuffer, IntPtr sQos,
            IntPtr gQos);
    }
}