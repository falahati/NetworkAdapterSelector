using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace NetworkAdapterSelector.Hook.Unmanaged
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    internal struct SocketAddress
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 14)] [FieldOffset(5)]
        internal readonly byte[] Data;

        [FieldOffset(0)] private ushort family;

        public AddressFamily Family
        {
            get => (AddressFamily) family;

            set => family = (ushort) value;
        }
    }
}