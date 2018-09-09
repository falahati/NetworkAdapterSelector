using System.Net;

namespace NetworkAdapterSelector.Hook.Unmanaged
{
    internal interface IAddressIn
    {
        IPAddress IPAddress { get; set; }
    }
}