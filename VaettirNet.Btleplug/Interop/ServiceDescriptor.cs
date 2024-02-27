using System.Runtime.InteropServices;

namespace VaettirNet.Btleplug.Interop;

[StructLayout(LayoutKind.Sequential)]
internal struct ServiceDescriptor
{
    public RemoteGuid Uuid;
    public int CharacteristicCount;
}