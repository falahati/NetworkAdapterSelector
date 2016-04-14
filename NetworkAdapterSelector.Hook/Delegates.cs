using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using NetworkAdapterSelector.Hook.Unmanaged;

namespace NetworkAdapterSelector.Hook
{
    internal static class Delegates
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        public delegate SocketError BindDelegate(IntPtr socket, ref SocketAddressIn address, int addressSize);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        public delegate SocketError ConnectDelegate(IntPtr socket, ref SocketAddressIn address, int addressSize);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        public delegate bool CreateProcessDelegate(
            IntPtr lpApplicationName,
            IntPtr lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            IntPtr lpCurrentDirectory,
            IntPtr lpStartupInfo,
            out ProcessInformation lpProcessInformation);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        public delegate bool SetWindowTextDelegate(IntPtr windowHandle, IntPtr textPointer);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        public delegate SocketError WsaConnectDelegate(
            IntPtr socket, ref SocketAddressIn address, int addressSize, IntPtr inBuffer, IntPtr outBuffer, IntPtr sQos,
            IntPtr gQos);
    }
}