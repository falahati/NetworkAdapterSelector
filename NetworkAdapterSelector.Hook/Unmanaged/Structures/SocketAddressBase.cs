using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace NetworkAdapterSelector.Hook.UnManaged.Structures
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    internal struct SocketAddressBase
    {
        [FieldOffset(0)] private ushort family;

        public AddressFamily Family
        {
            get => (AddressFamily) family;

            set => family = (ushort) value;
        }
    }
}