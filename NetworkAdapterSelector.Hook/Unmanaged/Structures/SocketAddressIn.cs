using System.Net.Sockets;
using System.Runtime.InteropServices;
using NetworkAdapterSelector.Hook.UnManaged.Interfaces;

namespace NetworkAdapterSelector.Hook.UnManaged.Structures
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    internal struct SocketAddressIn : ISocketAddress
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)] [FieldOffset(8)]
        private readonly byte[] Padding;

        [FieldOffset(4)] private AddressIn address;

        [FieldOffset(0)] private ushort family;

        [FieldOffset(2)] private short port;

        /// <inheritdoc />
        public int Port
        {
            get => System.Net.IPAddress.NetworkToHostOrder(port);
            set => port = System.Net.IPAddress.HostToNetworkOrder((short) value);
        }

        /// <inheritdoc />
        public IAddressIn Address
        {
            get => address;
            set => address = (AddressIn) value;
        }

        /// <inheritdoc />
        public AddressFamily AddressFamily
        {
            get => (AddressFamily) family;

            set => family = (ushort) value;
        }
    }
}