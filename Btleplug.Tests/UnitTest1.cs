using System;
using System.Threading.Tasks;
using VaettirNet.Btleplug;

namespace Btleplug.Tests;

public class Tests
{
    [Test]
    public async Task Test1()
    {
        var manager = BtleManager.Create();
        await foreach (BtlePeripheral p in manager.GetPeripherals([], false))
        {
            Console.WriteLine(p.GetId());
            p.Dispose();
        }
    }
}