using System.Net;
using System.Runtime.InteropServices;

namespace NetworkAdapterSelector.Hook.Unmanaged
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct AddressIn6 : IAddressIn
    {
        [FieldOffset(0)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        internal byte[] Bytes;

        public IPAddress IPAddress
        {
            get => new IPAddress(Bytes);
            set => Bytes = value.GetAddressBytes();
        }
    }
}