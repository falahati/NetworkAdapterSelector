using System.Net.Sockets;

namespace NetworkAdapterSelector.Hook.Unmanaged
{
    internal interface ISocketAddress
    {
        AddressFamily Family { get; set; }

        int Port { get; set; }

        IAddressIn Address { get; }
    }
}