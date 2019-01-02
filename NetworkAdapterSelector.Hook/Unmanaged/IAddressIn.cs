using System.Net;

namespace NetworkAdapterSelector.Hook.UnManaged
{
    internal interface IAddressIn
    {
        IPAddress IPAddress { get; set; }
    }
}