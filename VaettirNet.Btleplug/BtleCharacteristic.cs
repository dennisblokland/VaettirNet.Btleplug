using System;
using System.Collections.Immutable;
using VaettirNet.Btleplug.Interop;

namespace VaettirNet.Btleplug;

public class BtleCharacteristic
{
    public Guid Uuid { get; }
    public CharacteristicProperty Properties { get; }
    public ImmutableArray<Guid> Descriptors { get; }

    public BtleCharacteristic(Guid uuid, CharacteristicProperty properties, ImmutableArray<Guid> descriptors)
    {
        Uuid = uuid;
        Properties = properties;
        Descriptors = descriptors;
    }
}