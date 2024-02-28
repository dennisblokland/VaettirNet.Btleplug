using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
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

    private bool _eventsRegistered;
    private readonly object _eventRegistrationLock = new();
    private void EnsureCallbacks()
    {
        if (_eventsRegistered)
            return;
        lock (_eventRegistrationLock)
        {
            if (_eventsRegistered)
                return;
            
            NativeMethods.Call(_handle, h => NativeMethods.SetEventCallback(h, PeripheralFound, PeripheralDisconnected));
            _eventsRegistered = true;
        }
    }

    public event Action<ulong> OnDisconnected;
    private event Action<ulong, RemoteGuid[], PendingPeripheralHandle> OnFound;
    
    private void PeripheralDisconnected(ulong value)
    {
        OnDisconnected?.Invoke(value);
    }

    private int PeripheralFound(ulong addr, IntPtr handle, RemoteGuid[] services, int servicesCount)
    {
        Action<ulong, RemoteGuid[], PendingPeripheralHandle> found = OnFound;
        if (found == null)
            return 0;
        PendingPeripheralHandle pending = new(handle);
        found(addr, services, pending);
        return pending.IsClaimed ? 1 : 0;
    }

    private class PendingPeripheralHandle
    {
        private readonly object _lock = new();
        private readonly IntPtr _ptr;

        public PendingPeripheralHandle(IntPtr ptr)
        {
            _ptr = ptr;
        }

        public bool IsClaimed { get; private set; }

        public BtlePeripheralHandle Claim()
        {
            if (IsClaimed)
                throw new InvalidOperationException("Handle already claimed");
            lock (_lock)
            {
                if (IsClaimed)
                    throw new InvalidOperationException("Handle already claimed");
                IsClaimed = true;
            }

            return new BtlePeripheralHandle(_ptr);
        }
    }

    public IAsyncEnumerable<BtlePeripheral> GetPeripherals(Guid[] serviceFilter, bool includeServices, CancellationToken cancellationToken = default)
    {
        EnsureCallbacks();
        HashSet<ulong> found = [];
        var channel = Channel.CreateUnbounded<BtlePeripheral>();
        Action<ulong,RemoteGuid[],PendingPeripheralHandle> foundHandler = TryAcceptPeripheral;
        OnFound += foundHandler;
        NativeMethods.Call(_handle,
            h => NativeMethods.StartScan(
                h,
                serviceFilter.Select(RemoteGuid.FromGuid).ToArray(),
                serviceFilter.Length
            ));
        
        using CancellationTokenRegistration _ = cancellationToken.Register(() =>
        {
            OnFound -= foundHandler;
            NativeMethods.StopScan(_handle);
        });
        return channel.Reader.ReadAllAsync(cancellationToken);

        void TryAcceptPeripheral(ulong address, RemoteGuid[] services, PendingPeripheralHandle handle)
        {
            bool hasServices = services != null;
            if (includeServices != hasServices)
            {
                return;
            }

            if (!found.Add(address))
                return;
            
            ImmutableArray<Guid> g = [];
            if (services is { Length: > 0 })
            {
                g = services
                    .Select(s => s.ToGuid())
                    .ToImmutableArray();
            }

            channel.Writer.TryWrite(new BtlePeripheral(this, handle.Claim(), g, address));
        }
    }

    public void Dispose()
    {
        _handle.Dispose();
    }
}