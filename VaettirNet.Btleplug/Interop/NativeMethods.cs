using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using VaettirNet.Btleplug.SafeHandles;

namespace VaettirNet.Btleplug.Interop;

internal static partial class NativeMethods
{
    internal delegate void VoidCallback(BtleResult result);
    internal delegate void BooleanCallback(BtleResult result, int value);
    internal delegate void ULongValue(ulong value);
    internal delegate int PeripheralFoundCallback(ulong addr, IntPtr handle, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)]RemoteGuid[] services, int servicesCount);

    internal delegate void NotifyCallback(
        RemoteGuid uuid,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] data,
        int dataSize
    );
    
    private const string LibraryName = "btleplug_c";

    [LibraryImport(LibraryName, EntryPoint = "set_log_level")]
    public static partial void SetLogLevel(int level);
    
    [LibraryImport(LibraryName, EntryPoint = "create_module")]
    public static partial BtleResult CreateModule(out BtleModuleHandle handle);

    [LibraryImport(LibraryName, EntryPoint = "get_last_module_error", StringMarshalling = StringMarshalling.Utf8)]
    private static partial string GetLastError(BtleModuleHandle handle);

    [LibraryImport(LibraryName, EntryPoint = "set_event_callbacks")]
    public static partial BtleResult SetEventCallback(
        BtleModuleHandle handle,
        PeripheralFoundCallback callback,
        ULongValue disconnected
    );

    [LibraryImport(LibraryName, EntryPoint = "start_scan_peripherals")]
    public static partial BtleResult StartScan(
        BtleModuleHandle handle,
        [MarshalAs(UnmanagedType.LPArray)] RemoteGuid[] serviceFilter,
        int serviceFilterCount
    );
    
    [LibraryImport(LibraryName, EntryPoint = "stop_scan_peripherals")]
    public static partial BtleResult StopScan(BtleModuleHandle handle);

    [LibraryImport(LibraryName, EntryPoint = "peripheral_get_last_error", StringMarshalling = StringMarshalling.Utf8)]
    private static partial string GetLastError(BtlePeripheralHandle handle);

    [LibraryImport(LibraryName, EntryPoint = "peripheral_get_id")]
    public static partial BtleResult PeripheralGetId(BtlePeripheralHandle handle, out BtleStringHandle id);

    [LibraryImport(LibraryName, EntryPoint = "peripheral_is_connected")]
    public static partial BtleResult PeripheralGetIsConnected(BtlePeripheralHandle handle, BooleanCallback callback);

    [LibraryImport(LibraryName, EntryPoint = "peripheral_connect")]
    public static partial BtleResult PeripheralConnect(BtlePeripheralHandle handle, VoidCallback id);
    
    [LibraryImport(LibraryName, EntryPoint = "peripheral_disconnect")]
    public static partial BtleResult PeripheralDisconnect(BtlePeripheralHandle handle, VoidCallback id);

    [LibraryImport(LibraryName, EntryPoint = "peripheral_get_address")]
    public static partial BtleResult PeripheralGetAddress(BtlePeripheralHandle handle, out ulong address);

    [LibraryImport(LibraryName, EntryPoint = "peripheral_discover_services")]
    public static partial BtleResult PeripheralDiscoverServices(BtlePeripheralHandle handle, VoidCallback callback);

    [LibraryImport(LibraryName, EntryPoint = "peripheral_get_services")]
    public static partial BtleResult PeripheralGetServices(
        BtlePeripheralHandle handle,
        out IntPtr buffer
    );

    [LibraryImport(LibraryName, EntryPoint = "peripheral_register_notification_events")]
    public static partial BtleResult PeripheralRegisterNotificationCallback(
        BtlePeripheralHandle handle,
        VoidCallback readyCallback,
        NotifyCallback notifyCallback
    );
    
    [LibraryImport(LibraryName, EntryPoint = "peripheral_subscribe")]
    public static partial BtleResult PeripheralSubscribe(
        BtlePeripheralHandle handle,
        RemoteGuid serviceUuid,
        RemoteGuid characteristicUuid,
        VoidCallback callback
    );
    
    [LibraryImport(LibraryName, EntryPoint = "peripheral_unsubscribe")]
    public static partial BtleResult PeripheralUnsubscribe(
        BtlePeripheralHandle handle,
        RemoteGuid serviceUuid,
        RemoteGuid characteristicUuid,
        VoidCallback callback
    );
    
    [LibraryImport(LibraryName, EntryPoint = "peripheral_write")]
    public static partial BtleResult PeripheralWrite(
        BtlePeripheralHandle handle,
        RemoteGuid serviceUuid,
        RemoteGuid characteristicUuid,
        [MarshalAs(UnmanagedType.I4)] bool withResponse,
        IntPtr data,
        uint dataLength,
        VoidCallback callback
    );

    [LibraryImport(LibraryName, EntryPoint = "free_string")]
    public static partial BtleResult FreeString(IntPtr handle);

    [LibraryImport(LibraryName, EntryPoint = "free_module")]
    public static partial BtleResult FreeModule(IntPtr handle);

    [LibraryImport(LibraryName, EntryPoint = "free_peripheral")]
    public static partial BtleResult FreePeripheral(IntPtr handle);
    
    [LibraryImport(LibraryName, EntryPoint = "free_peripheral_services")]
    public static partial BtleResult FreePeripheralServices(IntPtr handle);

    [StackTraceHidden]
    public static void Call(BtleModuleHandle handle, Func<BtleModuleHandle, BtleResult> method)
    {
        ThrowIfError(handle, method(handle));
    }

    [StackTraceHidden]
    public static void Call(BtlePeripheralHandle handle, Func<BtlePeripheralHandle, BtleResult> method)
    {
        ThrowIfError(handle, method(handle));
    }

    [StackTraceHidden]
    public static async Task CallAsync(
        BtlePeripheralHandle handle,
        Func<BtlePeripheralHandle, VoidCallback, BtleResult> method
    )
    {
        // We need to run the continuations asynchronously, otherwise the continuation is still inside
        // Rust's async context, which means we can't call "get_last_error", because that causes a Rust panic
        // because it needs to lock the "last_error" value to return it, because Rust and multi-tasking are not friends
        TaskCompletionSource<BtleResult> src = new (TaskCreationOptions.RunContinuationsAsynchronously);
        VoidCallback c = src.SetResult;
        ThrowIfError(handle, method(handle, c));
        ThrowIfError(handle, await src.Task);
    }
    
    [StackTraceHidden]
    public static async Task<bool> CallAsync(
        BtlePeripheralHandle handle,
        Func<BtlePeripheralHandle, BooleanCallback, BtleResult> method
    )
    {
        // We need to run the continuations asynchronously, otherwise the continuation is still inside
        // Rust's async context, which means we can't call "get_last_error", because that causes a Rust panic
        // because it needs to lock the "last_error" value to return it, because Rust and multi-tasking are not friends
        TaskCompletionSource<(BtleResult result, int value)> src = new (TaskCreationOptions.RunContinuationsAsynchronously);
        BooleanCallback c = (r, v) => src.SetResult((r, v));
        ThrowIfError(handle, method(handle, c));
        (BtleResult result, int value) r = await src.Task;
        ThrowIfError(handle, r.result);
        return r.value != 0;
    }

    [StackTraceHidden]
    internal static void ThrowIfError(BtleModuleHandle handle, BtleResult res)
    {
        if (res == BtleResult.Success)
            return;
        string lastError = GetLastError(handle);
        throw BtleResultToException(res, lastError);
    }

    [StackTraceHidden]
    private static void ThrowIfError(BtlePeripheralHandle handle, BtleResult res)
    {
        if (res == BtleResult.Success)
            return;
        string lastError = GetLastError(handle);
        throw BtleResultToException(res, lastError);
    }

    [StackTraceHidden]
    private static Exception BtleResultToException(BtleResult result, string message)
    {
        return result switch
        {
            BtleResult.PermissionDenied => new BtlePermissionDeniedException(message),
            BtleResult.DeviceNotFound => new BtleDeviceNotFoundException(message),
            BtleResult.NotConnected => new BtleNotConnectedException(message),
            BtleResult.UnexpectedCallback => new BtleUnexpectedCallbackException(message),
            BtleResult.UnexpectedCharacteristic => new BtleUnexpectedCharacteristicException(message),
            BtleResult.NoSuchCharacteristic => new BtleNoSuchCharacteristicException(message),
            BtleResult.NotSupported => new BtleNotSupportedException(message),
            BtleResult.TimedOut => new BtleTimedOutException(message),
            BtleResult.Uuid => new BtleUuidException(message),
            BtleResult.InvalidBdAddr => new BtleInvalidBdAddrException(message),
            BtleResult.RuntimeError => new BtleRuntimeErrorException(message),
            
            BtleResult.Error => new BtleInteropException((BtleErrorCode)result, message),
            BtleResult.InvalidArgument => new ArgumentException(message),
            
            _ => throw new ArgumentOutOfRangeException(nameof(result), result, null)
        };
    }
}