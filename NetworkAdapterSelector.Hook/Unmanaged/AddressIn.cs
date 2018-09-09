using System;
using System.Net;
using System.Runtime.InteropServices;

namespace NetworkAdapterSelector.Hook.Unmanaged
{
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct AddressIn : IAddressIn
    {
        [FieldOffset(0)] internal uint Value;

        /// <inheritdoc />
        public IPAddress IPAddress
        {
            get => new IPAddress(Value);
            set => Value = BitConverter.ToUInt32(value.GetAddressBytes(), 0);
        }
    }
}