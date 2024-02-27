using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Channels;
using VaettirNet.Btleplug.Interop;
using VaettirNet.Btleplug.SafeHandles;

namespace VaettirNet.Btleplug;

public sealed class BtleManager : IDisposable
{
    private readonly BtleModuleHandle _handle;

    private BtleManager(BtleModuleHandle handle)
    {
        _handle = handle;
    }

    public static void SetLogLevel(BtleLogLevel level) => NativeMethods.SetLogLevel((int)level);

    public static BtleManager Create()
    {
        BtleResult res = NativeMethods.CreateModule(out BtleModuleHandle handle);
        NativeMethods.ThrowIfError(handle, res);
        return new BtleManager(handle);
    }

    public IAsyncEnumerable<BtlePeripheral> GetPeripherals(Guid[] serviceFilter, bool includeServices)
    {
        HashSet<ulong> found = [];
        var channel = Channel.CreateUnbounded<BtlePeripheral>();
        NativeMethods.Call(_handle,
            h => NativeMethods.StartScan(
                h,
                serviceFilter.Select(RemoteGuid.FromGuid).ToArray(),
                serviceFilter.Length,
                includeServices,
                PeripheralFound
            ));

        return channel.Reader.ReadAllAsync();
        
        void PeripheralFound(ulong address, IntPtr peripheralHandle, RemoteGuid[] services, int serviceCount)
        {
            if (!found.Add(address))
                return;
            
            ImmutableArray<Guid> g = [];
            if (services is { Length: > 0 })
            {
                g = services
                    .Select(s => s.ToGuid())
                    .ToImmutableArray();
            }

            channel.Writer.TryWrite(new BtlePeripheral(new BtlePeripheralHandle(peripheralHandle), g));
        }
    }

    public void Dispose()
    {
        _handle.Dispose();
    }
}