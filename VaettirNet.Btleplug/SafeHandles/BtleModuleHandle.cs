using System;
using Microsoft.Win32.SafeHandles;
using VaettirNet.Btleplug.Interop;

namespace VaettirNet.Btleplug.SafeHandles;

internal class BtleModuleHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public BtleModuleHandle() : base(true)
    {
    }

    protected override bool ReleaseHandle()
    {
        return NativeMethods.FreeModule(handle) == 0;
    }
}