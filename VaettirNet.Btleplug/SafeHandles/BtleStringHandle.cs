using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace VaettirNet.Btleplug.SafeHandles;

internal class BtleStringHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private readonly Lazy<string> _value;
    public string Value => _value.Value;
    
    internal BtleStringHandle(IntPtr handle) : base(true)
    {
        _value = new Lazy<string>(ParseValue);
        SetHandle(handle);
    }

    public BtleStringHandle() : base(true)
    {
        _value = new Lazy<string>(ParseValue);
    }

    private string ParseValue()
    {
        return Marshal.PtrToStringAnsi(handle);
    }

    protected override bool ReleaseHandle()
    {
        return Interop.NativeMethods.FreeString(handle) == 0;
    }
}