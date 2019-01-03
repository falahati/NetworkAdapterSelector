using System;
using System.Net;
using System.Runtime.InteropServices;
using NetworkAdapterSelector.Hook.UnManaged.Interfaces;

namespace NetworkAdapterSelector.Hook.UnManaged.Structures
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