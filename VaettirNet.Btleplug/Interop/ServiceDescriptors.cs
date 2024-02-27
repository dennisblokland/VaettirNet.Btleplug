using System.Runtime.InteropServices;

namespace VaettirNet.Btleplug.Interop;

[StructLayout(LayoutKind.Sequential)]
internal struct ServiceDescriptors
{
    public int ServiceCount;
}