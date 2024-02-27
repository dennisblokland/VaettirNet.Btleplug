namespace VaettirNet.Btleplug;

internal enum BtleResult : int
{
    Success = 0,
    Error = 1,
    InvalidArgument = 2,
    PermissionDenied = 101,
    DeviceNotFound = 102,
    NotConnected = 103,
    UnexpectedCallback = 104,
    UnexpectedCharacteristic = 105,
    NoSuchCharacteristic = 106,
    NotSupported = 107,
    TimedOut = 108,
    Uuid = 109,
    InvalidBdAddr = 110,
    RuntimeError = 111,
}