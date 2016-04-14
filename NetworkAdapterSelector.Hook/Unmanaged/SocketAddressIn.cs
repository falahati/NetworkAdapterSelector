using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace NetworkAdapterSelector.Hook.Unmanaged
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    internal struct SocketAddressIn
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)] [FieldOffset(8)] internal readonly byte[] Padding;

        [FieldOffset(4)] internal AddressIn IPAddress;

        [FieldOffset(0)] private ushort family;

        [FieldOffset(2)] private short port;

        internal int Port
        {
            get { return System.Net.IPAddress.NetworkToHostOrder(port); }
            set { port = System.Net.IPAddress.HostToNetworkOrder((short) value); }
        }

        internal AddressFamily Family
        {
            get { return (AddressFamily) family; }
            
            set { family = (ushort) value; }
        }

        [StructLayout(LayoutKind.Explicit, Size = 4)]
        public struct AddressIn
        {
            [FieldOffset(0)] internal uint Int;

            [FieldOffset(0)] internal readonly byte Byte1;

            [FieldOffset(1)] internal readonly byte Byte2;

            [FieldOffset(2)] internal readonly byte Byte3;

            [FieldOffset(3)] internal readonly byte Byte4;

            public IPAddress IpAddress
            {
                get { return new IPAddress(Int); }
                set { Int = BitConverter.ToUInt32(value.GetAddressBytes(), 0); }
            }
        }
    }
}