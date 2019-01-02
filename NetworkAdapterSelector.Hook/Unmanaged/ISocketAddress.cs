using System.Net.Sockets;

namespace NetworkAdapterSelector.Hook.UnManaged
{
    internal interface ISocketAddress
    {
        IAddressIn Address { get; }
        AddressFamily AddressFamily { get; set; }
        int Port { get; set; }
    }
}