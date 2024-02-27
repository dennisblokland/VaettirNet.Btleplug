using System.Runtime.InteropServices;

namespace VaettirNet.Btleplug.Interop;

[StructLayout(LayoutKind.Sequential)]
internal struct CharacteristicDescriptor
{
    public RemoteGuid Uuid;
    public CharacteristicProperty Properties;
    public int DescriptorCount;
}