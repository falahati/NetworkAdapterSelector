using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace NetworkAdapterSelector.Hook.Unmanaged
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct AddressInfo
    {
        internal AddressInfoHints Flags;

        internal AddressFamily Family;

        internal SocketType SocketType;

        internal ProtocolFamily Protocol;

        internal int AddressLen;

        internal IntPtr CanonName; // sbyte Array

        internal IntPtr Address; // byte Array

        internal IntPtr Next; // Next Element In AddressInfo Array

        [Flags]
        internal enum AddressInfoHints
        {
            None = 0,
            Passive = 0x01,
            Canonname = 0x02,
            Numerichost = 0x04,
            All = 0x0100,
            Addrconfig = 0x0400,
            V4Mapped = 0x0800,
            NonAuthoritative = 0x04000,
            Secure = 0x08000,
            ReturnPreferredNames = 0x010000,
            Fqdn = 0x00020000,
            Fileserver = 0x00040000
        }
    }
}