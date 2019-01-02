using System;
using System.Net;
using System.Runtime.InteropServices;

namespace NetworkAdapterSelector.Hook.UnManaged
{
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    internal struct AddressIn : IAddressIn
    {
        [FieldOffset(0)] private uint Value;

        /// <inheritdoc />
        public IPAddress IPAddress
        {
            get => new IPAddress(Value);
            set => Value = BitConverter.ToUInt32(value.GetAddressBytes(), 0);
        }
    }
}