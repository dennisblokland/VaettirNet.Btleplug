using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using VaettirNet.Btleplug.Interop;
using VaettirNet.Btleplug.SafeHandles;

namespace VaettirNet.Btleplug;

public sealed class BtlePeripheral : IDisposable
{
    public ImmutableArray<Guid> ServiceUuids { get; }
    public ulong Address { get; }

    private readonly BtleManager _manager;
    private readonly BtlePeripheralHandle _handle;
    private readonly NativeMethods.NotifyCallback _notifyCallbackDelegate;
    private bool _discoveredServices;
    private ImmutableArray<BtleService> _services;
    private GCHandle? _notifyCallbackHandle;

    public event Action<BtlePeripheral> Disconnected;

    internal BtlePeripheral(
        BtleManager manager,
        BtlePeripheralHandle handle,
        ImmutableArray<Guid> serviceUuids,
        ulong address)
    {
        ServiceUuids = serviceUuids;
        Address = address;
        _manager = manager;
        _handle = handle;
        manager.OnDisconnected += OnDisconnected;
        _notifyCallbackDelegate = OnNotifyDataReceived;
        _notifyCallbackHandle = GCHandle.Alloc(_notifyCallbackDelegate);
    }

    private void OnDisconnected(ulong addr)
    {
        if (addr != Address)
            return;

        Disconnected?.Invoke(this);
    }

    public void Dispose()
    {
        _manager.OnDisconnected -= OnDisconnected;
        _handle.Dispose();
        if (_notifyCallbackHandle.HasValue && _notifyCallbackHandle.Value.IsAllocated)
        {
            _notifyCallbackHandle.Value.Free();
        }
    }

    public string GetId()
    {
        BtleStringHandle id = default;
        NativeMethods.Call(_handle, h => NativeMethods.PeripheralGetId(h, out id));
        string value = id.Value;
        id.Dispose();
        return value;
    }

    public async Task<IEnumerable<BtleService>> GetServicesAsync()
    {
        if (!_discoveredServices)
        {
            _discoveredServices = true;
            await NativeMethods.CallAsync(_handle, NativeMethods.PeripheralDiscoverServices);
        }

        IntPtr buffer = default;
        try
        {
            NativeMethods.Call(_handle, h => NativeMethods.PeripheralGetServices(h, out buffer));
            IntPtr readPtr = buffer + 8;
            var serviceDescriptors = Read<ServiceDescriptors>(ref readPtr);
            var services = new BtleService[serviceDescriptors.ServiceCount];
            for (var iService = 0; iService < serviceDescriptors.ServiceCount; iService++)
            {
                var serviceDescriptor = Read<ServiceDescriptor>(ref readPtr);
                var characteristics = new BtleCharacteristic[serviceDescriptor.CharacteristicCount];
                for (var iCharacteristic = 0; iCharacteristic < serviceDescriptor.CharacteristicCount; iCharacteristic++)
                {
                    var characteristicDescriptor = Read<CharacteristicDescriptor>(ref readPtr);
                    var descriptors = new Guid[characteristicDescriptor.DescriptorCount];
                    for (var iDescriptor = 0; iDescriptor < characteristicDescriptor.DescriptorCount; iDescriptor++)
                    {
                        var uuid = Read<RemoteGuid>(ref readPtr);
                        descriptors[iDescriptor] = uuid.ToGuid();
                    }

                    characteristics[iCharacteristic] = new BtleCharacteristic(
                        characteristicDescriptor.Uuid.ToGuid(),
                        characteristicDescriptor.Properties,
                        descriptors.ToImmutableArray()
                    );
                }

                services[iService] = new BtleService(serviceDescriptor.Uuid.ToGuid(), characteristics.ToImmutableArray());
            }

            _services = services.ToImmutableArray();
            if (NativeMethods.FreePeripheralServices(buffer) != BtleResult.Success)
            {
                throw new BtleRuntimeErrorException("Unable to free peripheral service");
            }
            return _services;
        }
        catch
        {
            if (buffer != IntPtr.Zero && NativeMethods.FreePeripheralServices(buffer) != BtleResult.Success)
            {
                // We are already unwinding, and can't free the buffer, we just leaked!!!
            }

            throw;
        }
    }

    private static T Read<T>(ref IntPtr ptr)
    {
        var value = Marshal.PtrToStructure<T>(ptr);
        ptr += Marshal.SizeOf<T>();
        return value;
    }

    public async Task<bool> IsConnectedAsync()
    {
        return await NativeMethods.CallAsync(_handle, NativeMethods.PeripheralGetIsConnected);
    }

    public async Task ConnectAsync()
    {
        await NativeMethods.CallAsync(_handle, NativeMethods.PeripheralConnect);
    }

    public async Task DisconnectAsync()
    {
        await NativeMethods.CallAsync(_handle, NativeMethods.PeripheralDisconnect);
    }

    public delegate void PeripheralNotifyDataReceivedCallback(
        BtlePeripheral peripheral,
        Guid service,
        Guid characteristic,
        Span<byte> data
    );

    private readonly SemaphoreSlim _callbackSemaphore = new(1, 1);
    private Dictionary<Guid, PeripheralNotifyDataReceivedCallback> _callbacks = null;

    public async ValueTask RegisterNotificationCallback(
        Guid service,
        Guid characteristic,
        PeripheralNotifyDataReceivedCallback callback
    )
    {
        if (_callbacks == null)
        {
            _callbacks = [];
            await NativeMethods.CallAsync(_handle,
                (h, c) => NativeMethods.PeripheralRegisterNotificationCallback(h, c, _notifyCallbackDelegate));
        }

        await _callbackSemaphore.WaitAsync();
        try
        {
            if (_callbacks.TryGetValue(characteristic, out var old))
            {
                _callbacks[characteristic] = old + callback;
            }
            else
            {
                await NativeMethods.CallAsync(_handle,
                    (h, c) => NativeMethods.PeripheralSubscribe(h,
                        RemoteGuid.FromGuid(service),
                        RemoteGuid.FromGuid(characteristic),
                        c));
                _callbacks[characteristic] = callback;
            }
        }
        finally
        {
            _callbackSemaphore.Release();
        }
    }

    public async ValueTask UnregisterNotificationCallback(
        Guid service,
        Guid characteristic,
        PeripheralNotifyDataReceivedCallback callback
    )
    {
        await _callbackSemaphore.WaitAsync();
        try
        {
            if (_callbacks.TryGetValue(characteristic, out var old))
            {
                PeripheralNotifyDataReceivedCallback peripheralNotifyDataReceivedCallback = old - callback;
                if (peripheralNotifyDataReceivedCallback == null)
                {
                    await NativeMethods.CallAsync(_handle,
                        (h, c) => NativeMethods.PeripheralUnsubscribe(h,
                            RemoteGuid.FromGuid(service),
                            RemoteGuid.FromGuid(characteristic),
                            c));
                    _callbacks.Remove(characteristic);
                }
                else
                {
                    _callbacks[characteristic] = peripheralNotifyDataReceivedCallback;
                }
            }
        }
        finally
        {
            _callbackSemaphore.Release();
        }
    }

    private void OnNotifyDataReceived(RemoteGuid uuid, byte[] data, int dataSize)
    {
        var guid = uuid.ToGuid();
        _callbackSemaphore.Wait();
        PeripheralNotifyDataReceivedCallback callback;
        try
        {
            callback = _callbacks.GetValueOrDefault(guid);
        }
        finally
        {
            _callbackSemaphore.Release();
        }
        callback?.Invoke(this, guid, guid, data.AsSpan());
    }

    public async Task Write(Guid service, Guid characteristic, ReadOnlyMemory<byte> data, bool withResponse)
    {
        using MemoryHandle dataHandle = data.Pin();
        await NativeMethods.CallAsync(_handle,
            (h, c) =>
            {
                unsafe
                {
                    return NativeMethods.PeripheralWrite(h,
                        RemoteGuid.FromGuid(service),
                        RemoteGuid.FromGuid(characteristic),
                        withResponse,
                        (IntPtr)dataHandle.Pointer,
                        (uint)data.Length,
                        c);
                }
            });
    }
}