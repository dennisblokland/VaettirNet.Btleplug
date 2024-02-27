using System;

namespace VaettirNet.Btleplug.Interop;

[Flags]
public enum CharacteristicProperty : byte
{

    Broadcast = 0x01,
    Read = 0x02,
    WriteWithoutResponse = 0x04,
    Write = 0x08,
    Notify = 0x10,
    Indicate = 0x20,
    AuthenticatedSignedWrites = 0x40,
    ExtendedProperties = 0x80,
}