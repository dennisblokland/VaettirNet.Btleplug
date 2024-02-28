using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VaettirNet.Btleplug;
using VaettirNet.Btleplug.Interop;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        BtleManager.SetLogLevel(BtleLogLevel.Debug);
        var manager = BtleManager.Create();
        List<BtlePeripheral> all = [];
        CancellationTokenSource src = new();
        src.CancelAfter(TimeSpan.FromSeconds(30));
        try
        {
            await ScanPeripherals(manager, src, all);
        }
        catch (OperationCanceledException)
        {
        }
        
        Console.WriteLine("Shutting down");

        foreach (BtlePeripheral p in all)
        {
            await p.DisconnectAsync();
            p.Dispose();
        }
    }

    private static async Task ScanPeripherals(BtleManager manager, CancellationTokenSource src, List<BtlePeripheral> all)
    {
        await foreach (BtlePeripheral p in manager.GetPeripherals([Guid.Parse("6e400001-b5a3-f393-e0a9-e50e24dcca9e")], includeServices: true, src.Token))
        {
            all.Add(p);
            Console.WriteLine($"Found device: {p.GetId()}");
            await p.ConnectAsync();
            bool isConnected = await p.IsConnectedAsync();
            Console.WriteLine($"Connected ({isConnected})");
            foreach (BtleService service in await p.GetServicesAsync())
            {
                Console.WriteLine($"  S: {service.Uuid}");
                foreach (BtleCharacteristic c in service.Characteristics)
                {
                    Console.WriteLine($"  \u2514 C : {c.Uuid} {c.Properties}");
                    foreach (Guid d in c.Descriptors)
                    {
                        Console.WriteLine($"    \u2514 D : {d}");
                    }

                    if (c.Properties.HasFlag(CharacteristicProperty.Notify))
                    {
                        await p.RegisterNotificationCallback(service.Uuid, c.Uuid, NotifyFound);
                    }

                    if (c.Properties.HasFlag(CharacteristicProperty.WriteWithoutResponse))
                    {
                        await p.Write(service.Uuid, c.Uuid, new[] { (byte)0x01 }, false);
                    }
                }
            }
        }
    }

    private static void NotifyFound(BtlePeripheral peripheral, Guid service, Guid characteristic, Span<byte> data)
    {
        Console.WriteLine($"Got notification {service}:{characteristic} of size {data.Length}");
    }
}