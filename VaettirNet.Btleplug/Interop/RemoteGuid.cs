using System;
using System.Runtime.InteropServices;

namespace VaettirNet.Btleplug.Interop;

[StructLayout(LayoutKind.Sequential)]
public struct RemoteGuid
{
    public unsafe fixed byte Bytes[16];

    public Guid ToGuid()
    {
        unsafe
        {
            fixed (byte* b = Bytes)
            {
                return new Guid(new ReadOnlySpan<byte>(b, 16), bigEndian: true);
            }
        }
    }

    public static RemoteGuid Parse(string s)
    {
        return FromGuid(Guid.Parse(s));
    }

    public static RemoteGuid FromGuid(Guid g)
    {
        var r = new RemoteGuid();
        unsafe
        {
            g.TryWriteBytes(new Span<byte>(r.Bytes, 16), bigEndian: true, out _);
        }

        return r;
    }
}