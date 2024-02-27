using System;

namespace VaettirNet.Btleplug;

public class BtleInteropException : Exception
{
    public BtleErrorCode ErrorCode { get; }

    public BtleInteropException(BtleErrorCode errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }

    public BtleInteropException(BtleErrorCode errorCode, string message, Exception innerException) : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}

public class BtleNotConnectedException : BtleInteropException
{
    public BtleNotConnectedException(string message) : base(BtleErrorCode.NotConnected, message)
    {
    }

    public BtleNotConnectedException(string message, Exception innerException) : base(BtleErrorCode.NotConnected, message, innerException)
    {
    }
}

public class BtlePermissionDeniedException : BtleInteropException
{
    public BtlePermissionDeniedException(string message) : base(BtleErrorCode.PermissionDenied, message)
    {
    }

    public BtlePermissionDeniedException(string message, Exception innerException) : base(BtleErrorCode.PermissionDenied, message, innerException)
    {
    }
}

public class BtleDeviceNotFoundException : BtleInteropException
{
    public BtleDeviceNotFoundException(string message) : base(BtleErrorCode.DeviceNotFound, message)
    {
    }

    public BtleDeviceNotFoundException(string message, Exception innerException) : base(BtleErrorCode.DeviceNotFound, message, innerException)
    {
    }
}

public class BtleUnexpectedCallbackException : BtleInteropException
{
    public BtleUnexpectedCallbackException(string message) : base(BtleErrorCode.UnexpectedCallback, message)
    {
    }

    public BtleUnexpectedCallbackException(string message, Exception innerException) : base(BtleErrorCode.UnexpectedCallback, message, innerException)
    {
    }
}

public class BtleUnexpectedCharacteristicException : BtleInteropException
{
    public BtleUnexpectedCharacteristicException(string message) : base(BtleErrorCode.UnexpectedCharacteristic, message)
    {
    }

    public BtleUnexpectedCharacteristicException(string message, Exception innerException) : base(BtleErrorCode.UnexpectedCharacteristic, message, innerException)
    {
    }
}

public class BtleNoSuchCharacteristicException : BtleInteropException
{
    public BtleNoSuchCharacteristicException(string message) : base(BtleErrorCode.NoSuchCharacteristic, message)
    {
    }

    public BtleNoSuchCharacteristicException(string message, Exception innerException) : base(BtleErrorCode.NoSuchCharacteristic, message, innerException)
    {
    }
}

public class BtleNotSupportedException : BtleInteropException
{
    public BtleNotSupportedException(string message) : base(BtleErrorCode.NotSupported, message)
    {
    }

    public BtleNotSupportedException(string message, Exception innerException) : base(BtleErrorCode.NotSupported, message, innerException)
    {
    }
}

public class BtleTimedOutException : BtleInteropException
{
    public BtleTimedOutException(string message) : base(BtleErrorCode.TimedOut, message)
    {
    }

    public BtleTimedOutException(string message, Exception innerException) : base(BtleErrorCode.TimedOut, message, innerException)
    {
    }
}

public class BtleUuidException : BtleInteropException
{
    public BtleUuidException(string message) : base(BtleErrorCode.Uuid, message)
    {
    }

    public BtleUuidException(string message, Exception innerException) : base(BtleErrorCode.Uuid, message, innerException)
    {
    }
}

public class BtleInvalidBdAddrException : BtleInteropException
{
    public BtleInvalidBdAddrException(string message) : base(BtleErrorCode.InvalidBdAddr, message)
    {
    }

    public BtleInvalidBdAddrException(string message, Exception innerException) : base(BtleErrorCode.InvalidBdAddr, message, innerException)
    {
    }
}

public class BtleRuntimeErrorException : BtleInteropException
{
    public BtleRuntimeErrorException(string message) : base(BtleErrorCode.RuntimeError, message)
    {
    }

    public BtleRuntimeErrorException(string message, Exception innerException) : base(BtleErrorCode.RuntimeError, message, innerException)
    {
    }
}

public enum BtleErrorCode
{
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