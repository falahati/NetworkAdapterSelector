using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace NetworkAdapterSelector.Hook.UnManaged
{
    internal static class Socket
    {
        [DllImport("ws2_32", SetLastError = true, EntryPoint = "bind")]
        public static extern SocketError Bind(IntPtr socket, ref SocketAddressIn address, int addressSize);

        [DllImport("ws2_32", SetLastError = true, EntryPoint = "bind")]
        public static extern SocketError Bind(IntPtr socket, IntPtr address, int addressSize);

        [DllImport("ws2_32", SetLastError = true, EntryPoint = "bind")]
        public static extern SocketError Bind(IntPtr socket, ref SocketAddressIn6 address, int addressSize);
        
        [DllImport("ws2_32", SetLastError = true, EntryPoint = "connect")]
        public static extern SocketError Connect(IntPtr socket, IntPtr address, int addressSize);
        
        [DllImport("ws2_32", SetLastError = true)]
        // ReSharper disable once TooManyArguments
        public static extern SocketError WSAConnect(
            IntPtr socket,
            IntPtr address,
            int addressSize,
            IntPtr inBuffer,
            IntPtr outBuffer,
            IntPtr sQos,
            IntPtr gQos);

        [DllImport("ws2_32", SetLastError = false)]
        public static extern SocketError WSAGetLastError();

        [DllImport("ws2_32", SetLastError = false)]
        public static extern void WSASetLastError(SocketError error);
    }
}