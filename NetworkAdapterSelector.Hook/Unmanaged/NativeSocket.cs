using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using NetworkAdapterSelector.Hook.UnManaged.Structures;

namespace NetworkAdapterSelector.Hook.UnManaged
{
    internal static class NativeSocket
    {
        [DllImport("ws2_32", SetLastError = true, EntryPoint = "bind")]
        public static extern SocketError Bind(IntPtr socket, ref SocketAddressIn address, int addressSize);

        [DllImport("ws2_32", SetLastError = true, EntryPoint = "bind")]
        public static extern SocketError Bind(IntPtr socket, IntPtr address, int addressSize);

        [DllImport("ws2_32", SetLastError = true, EntryPoint = "bind")]
        public static extern SocketError Bind(IntPtr socket, ref SocketAddressIn6 address, int addressSize);

        [DllImport("ws2_32", SetLastError = true, EntryPoint = "closesocket")]
        public static extern SocketError CloseSocket(IntPtr socket);

        [DllImport("ws2_32", SetLastError = true, EntryPoint = "connect")]
        public static extern SocketError Connect(IntPtr socket, IntPtr address, int addressSize);

        [DllImport("ws2_32", SetLastError = true, EntryPoint = "socket")]
        public static extern IntPtr OpenSocket(AddressFamily addressFamily, SocketType type, ProtocolType protocol);

        [DllImport("ws2_32", SetLastError = true, EntryPoint = "WSAConnect")]
        // ReSharper disable once TooManyArguments
        public static extern SocketError WSAConnect(
            IntPtr socket,
            IntPtr address,
            int addressSize,
            IntPtr inBuffer,
            IntPtr outBuffer,
            IntPtr sQos,
            IntPtr gQos
        );

        [DllImport("ws2_32", SetLastError = false, EntryPoint = "WSAGetLastError")]
        public static extern SocketError WSAGetLastError();

        [DllImport("ws2_32", SetLastError = false, EntryPoint = "WSASetLastError")]
        public static extern void WSASetLastError(SocketError error);

        [DllImport("ws2_32", SetLastError = true, EntryPoint = "WSASocketA")]
        // ReSharper disable once TooManyArguments
        public static extern IntPtr WSAOpenSocketA(
            AddressFamily addressFamily,
            SocketType type,
            ProtocolType protocol,
            IntPtr protocolInfo,
            int groupId,
            short flags
        );

        [DllImport("ws2_32", SetLastError = true, EntryPoint = "WSASocketW")]
        // ReSharper disable once TooManyArguments
        public static extern IntPtr WSAOpenSocketW(
            AddressFamily addressFamily,
            SocketType type,
            ProtocolType protocol,
            IntPtr protocolInfo,
            int groupId,
            short flags
        );
    }
}