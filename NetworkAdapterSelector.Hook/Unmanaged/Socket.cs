using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace NetworkAdapterSelector.Hook.Unmanaged
{
    internal static class Socket
    {
        [DllImport("ws2_32", SetLastError = true)]
        public static extern SocketError WSAConnect(IntPtr socket, ref SocketAddressIn address, int addressSize,
            IntPtr inBuffer, IntPtr outBuffer, IntPtr sQos, IntPtr gQos);

        [DllImport("ws2_32", SetLastError = true, EntryPoint = "connect")]
        public static extern SocketError Connect(IntPtr socket, ref SocketAddressIn address, int addressSize);

        [DllImport("ws2_32", SetLastError = true, EntryPoint = "bind")]
        public static extern SocketError Bind(IntPtr socket, ref SocketAddressIn address, int addressSize);

        [DllImport("ws2_32", SetLastError = true)]
        public static extern SocketError WSAConnect(IntPtr socket, ref SocketAddressIn6 address, int addressSize,
            IntPtr inBuffer, IntPtr outBuffer, IntPtr sQos, IntPtr gQos);

        [DllImport("ws2_32", SetLastError = true, EntryPoint = "connect")]
        public static extern SocketError Connect(IntPtr socket, ref SocketAddressIn6 address, int addressSize);

        [DllImport("ws2_32", SetLastError = true, EntryPoint = "bind")]
        public static extern SocketError Bind(IntPtr socket, ref SocketAddressIn6 address, int addressSize);

        [DllImport("ws2_32", SetLastError = true)]
        public static extern SocketError WSAGetLastError();
    }
}