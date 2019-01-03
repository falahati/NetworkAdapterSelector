using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using NetworkAdapterSelector.Hook.UnManaged.Interfaces;

namespace NetworkAdapterSelector.Hook.UnManaged.Structures
{
    [StructLayout(LayoutKind.Explicit, Size = 28)]
    internal struct SocketAddressIn6 : ISocketAddress
    {
        [FieldOffset(8)] private AddressIn6 address;

        [FieldOffset(0)] private ushort family;

        [FieldOffset(2)] private short port;

        [FieldOffset(4)] private uint flowInfo;

        [FieldOffset(24)] private uint scopeId;

        /// <inheritdoc />
        public int Port
        {
            get => IPAddress.NetworkToHostOrder(port);
            set => port = IPAddress.HostToNetworkOrder((short) value);
        }

        public uint FlowInfo
        {
            get => flowInfo;
            set => flowInfo = value;
        }

        public uint ScopeId
        {
            get => scopeId;
            set => scopeId = value;
        }

        /// <inheritdoc />
        public AddressFamily AddressFamily
        {
            get => (AddressFamily) family;

            set => family = (ushort) value;
        }

        /// <inheritdoc />
        public IAddressIn Address
        {
            get => address;
            set => address = (AddressIn6) value;
        }
    }
}