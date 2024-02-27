# VaettirNet.BtlePlug

A library for interacting with BLE devices with .NET 

## Example usage

```csharp
var manager = BtleManager.Create();
await foreach (BtlePeripheral p in manager.GetPeripherals([Guid.Parse("6e400001-b5a3-f393-e0a9-e50e24dcca9e")], includeServices: true))
{
    Console.WriteLine($"Found device: {p.GetId()}");
    await p.ConnectAsync();
    foreach (BtleService service in await p.GetServicesAsync())
    {
        foreach (BtleCharacteristic c in service.Characteristics)
        {
            if (c.Properties.HasFlag(CharacteristicProperty.Notify))
            {
                await p.RegisterNotificationCallback(service.Uuid, c.Uuid, (_, service, characteristic, data) => 
                    Console.WriteLine($"Got notification {service}:{characteristic} of size {data.Length}")
                );
            }

            if (c.Properties.HasFlag(CharacteristicProperty.WriteWithoutResponse))
            {
                await p.Write(service.Uuid, c.Uuid, new[] { (byte)0x01 }, false);
            }
        }
    }
}
```