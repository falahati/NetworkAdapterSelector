using System.Net;

namespace NetworkAdapterSelector.Hook.UnManaged.Interfaces
{
    internal interface IAddressIn
    {
        IPAddress IPAddress { get; set; }
    }
}