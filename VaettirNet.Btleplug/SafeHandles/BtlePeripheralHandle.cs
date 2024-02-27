using System;
using Microsoft.Win32.SafeHandles;

namespace VaettirNet.Btleplug.SafeHandles;

internal class BtlePeripheralHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    internal BtlePeripheralHandle(IntPtr handle) : base(true)
    {
        SetHandle(handle);
    }
    
    public BtlePeripheralHandle() : base(true)
    {
    }

    protected override bool ReleaseHandle()
    {
        return Interop.NativeMethods.FreePeripheral(handle) == 0;
    }
}