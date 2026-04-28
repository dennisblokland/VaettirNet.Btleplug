# VaettirNet.Btleplug

[![Build Status](https://github.com/ChadNedzlek/VaettirNet.Btleplug/actions/workflows/main-build.yml/badge.svg)](https://github.com/{username}/{repository}/actions/workflows/main-build.yml)
[![NuGet](https://img.shields.io/nuget/v/VaettirNet.Btleplu.svg)](https://www.nuget.org/packages/VaettirNet.Btleplu/)

## Overview
VaettirNet.Btleplug is a .NET wrapper for [BtlePlug](https://github.com/deviceplug/btleplug), providing a convenient way to work with Bluetooth Low Energy (BLE) devices in .NET applications. This library enables seamless BLE communication across multiple platforms, including Windows, Linux, and macOS.
## Features
- Cross-platform BLE support (Windows, Linux, macOS)
- Device discovery and connection management
- Service and characteristic enumeration
- Read and write operations for BLE characteristics
- Notification subscription for real-time data updates
- Simple, intuitive API for BLE communication

## Installation
Install the package via NuGet:

```bash
dotnet add package VaettirNet.Btleplug
```

## Usage Example
``` csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VaettirNet.Btleplug;
using VaettirNet.Btleplug.Interop;

// Create a BLE manager
BtleManager.SetLogLevel(BtleLogLevel.Debug);
var manager = BtleManager.Create();

// Scan for devices with a specific service UUID
var serviceUuid = Guid.Parse("6e400001-b5a3-f393-e0a9-e50e24dcca9e");
CancellationTokenSource cts = new();
cts.CancelAfter(TimeSpan.FromSeconds(30));

// Discover and connect to peripherals
await foreach (BtlePeripheral peripheral in manager.GetPeripherals([serviceUuid], includeServices: true, cts.Token))
{
    Console.WriteLine($"Found device: {peripheral.GetId()}");
    
    // Connect to the peripheral
    await peripheral.ConnectAsync();
    bool isConnected = await peripheral.IsConnectedAsync();
    
    // Discover services and characteristics
    foreach (BtleService service in await peripheral.GetServicesAsync())
    {
        Console.WriteLine($"Service: {service.Uuid}");
        
        foreach (BtleCharacteristic characteristic in service.Characteristics)
        {
            Console.WriteLine($"Characteristic: {characteristic.Uuid} with properties: {characteristic.Properties}");
            
            // Register for notifications if supported
            if (characteristic.Properties.HasFlag(CharacteristicProperty.Notify))
            {
                await peripheral.RegisterNotificationCallback(
                    service.Uuid, 
                    characteristic.Uuid, 
                    (p, s, c, data) => {
                        Console.WriteLine($"Notification received: {data.Length} bytes");
                    }
                );
            }
        }
    }
    
    // Don't forget to disconnect and dispose when done
    await peripheral.DisconnectAsync();
    peripheral.Dispose();
}
```
## Platform Support
VaettirNet.Btleplug supports the following platforms:
- Windows (x64)
- Linux (x64)
- macOS (x64, arm64)

The library automatically loads the appropriate native dependencies for your platform.
## License
This project is licensed under the [MIT License](LICENSE.md).
## Related Projects
- [btleplug](https://github.com/deviceplug/btleplug) - The Rust BLE library that powers this wrapper
- Author: [Chad Nedzlek](https://github.com/ChadNedzlek)

## Contributing
Contributions are welcome! Please feel free to submit a Pull Request.
