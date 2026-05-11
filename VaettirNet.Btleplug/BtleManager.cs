using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
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
    // Native side keeps raw fn pointers to these delegates after SetEventCallback returns.
    // Hold strong instance references AND pin via GCHandle so they survive GC compaction
    // for the lifetime of this BtleManager. Without this the runtime aborts with
    // "A callback was made on a garbage collected delegate" when the scan fires later.
    private NativeMethods.PeripheralFoundCallback _peripheralFoundDelegate;
    private NativeMethods.ULongValue _peripheralDisconnectedDelegate;
    private GCHandle _peripheralFoundHandle;
    private GCHandle _peripheralDisconnectedHandle;
    private void EnsureCallbacks()
    {
        if (_eventsRegistered)
            return;
        lock (_eventRegistrationLock)
        {
            if (_eventsRegistered)
                return;

            _peripheralFoundDelegate = PeripheralFound;
            _peripheralDisconnectedDelegate = PeripheralDisconnected;
            _peripheralFoundHandle = GCHandle.Alloc(_peripheralFoundDelegate);
            _peripheralDisconnectedHandle = GCHandle.Alloc(_peripheralDisconnectedDelegate);

            NativeMethods.Call(_handle, h => NativeMethods.SetEventCallback(h, _peripheralFoundDelegate, _peripheralDisconnectedDelegate));
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

    public async IAsyncEnumerable<BtlePeripheral> GetPeripherals(Guid[] serviceFilter, bool includeServices, [EnumeratorCancellation] CancellationToken cancellationToken = default)
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
        
        try
        {
            await foreach (BtlePeripheral peripheral in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return peripheral;
            }
        }
        finally
        {
            OnFound -= foundHandler;
            NativeMethods.StopScan(_handle);
        }

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
        // Dispose the native handle first so the Rust side stops invoking callbacks,
        // then release the GCHandles that kept the delegates alive.
        _handle.Dispose();

        if (_peripheralFoundHandle.IsAllocated)
            _peripheralFoundHandle.Free();
        if (_peripheralDisconnectedHandle.IsAllocated)
            _peripheralDisconnectedHandle.Free();

    }
}