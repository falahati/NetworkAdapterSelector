using System.Net;
using System.Runtime.InteropServices;
using NetworkAdapterSelector.Hook.UnManaged.Interfaces;

namespace NetworkAdapterSelector.Hook.UnManaged.Structures
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    internal struct AddressIn6 : IAddressIn
    {
        [FieldOffset(0)] [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        private byte[] Bytes;

        public IPAddress IPAddress
        {
            get => new IPAddress(Bytes);
            set => Bytes = value.GetAddressBytes();
        }
    }
}