using System;
using System.Collections.Immutable;

namespace VaettirNet.Btleplug;

public class BtleService
{
    public Guid Uuid { get; }
    public ImmutableArray<BtleCharacteristic> Characteristics { get; }

    public BtleService(Guid uuid, ImmutableArray<BtleCharacteristic> characteristics)
    {
        Uuid = uuid;
        Characteristics = characteristics;
    }
}