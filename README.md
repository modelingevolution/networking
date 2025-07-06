# ModelingEvolution.NetworkManager

[![NuGet](https://img.shields.io/nuget/v/ModelingEvolution.NetworkManager.svg)](https://www.nuget.org/packages/ModelingEvolution.NetworkManager/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download)

A comprehensive .NET wrapper for the NetworkManager D-Bus interface, providing powerful network management capabilities for Linux systems. This library enables full control over WiFi, Ethernet, bridging, VPN, and advanced network configuration through a type-safe, async-first API.

## Features

- 🌐 **Complete NetworkManager D-Bus Wrapper**: Full access to all NetworkManager functionality
- 📶 **WiFi Management**: Connect, scan, manage access points and wireless networks
- 🔌 **Ethernet Support**: Wired network configuration and monitoring
- 🌉 **Bridge Configuration**: Create and manage network bridges
- 🔒 **VPN Integration**: Support for VPN connections and profiles
- ⚡ **Async/Await Support**: Modern async programming patterns throughout
- 🎯 **Type Safety**: Strongly-typed interfaces with full IntelliSense support
- 📊 **Real-time Monitoring**: Event-driven network state changes and device monitoring
- 🔧 **IPv4/IPv6 Support**: Complete IP configuration management
- 🚀 **High Performance**: Efficient D-Bus communication with minimal overhead

## Installation

```bash
dotnet add package ModelingEvolution.NetworkManager
```

## Quick Start

### Basic Network Client

```csharp
using ModelingEvolution.NetworkManager;

// Create NetworkManager client
using var client = await NetworkManagerClient.Create();

// Get all network devices
var devices = await client.GetDevices();
foreach (var device in devices)
{
    Console.WriteLine($"Device: {device.InterfaceName} ({device.DeviceType})");
    Console.WriteLine($"State: {device.State}");
    
    // Get connection info if connected
    var connectionInfo = await device.GetConnectionInfo();
    if (connectionInfo != null)
    {
        Console.WriteLine($"IP: {connectionInfo.Ip4Config}");
    }
}
```

### WiFi Network Management

```csharp
using ModelingEvolution.NetworkManager;

using var client = await NetworkManagerClient.Create();

// Get WiFi devices
var wifiDevices = await client.GetWifiDevices();

foreach (var wifiDevice in wifiDevices)
{
    Console.WriteLine($"WiFi Device: {wifiDevice.InterfaceName}");
    
    // Scan for available networks
    var networks = await client.GetAccessPoints().ToListAsync();
    
    foreach (var network in networks.Take(5))
    {
        Console.WriteLine($"  SSID: {network.Ssid}");
        Console.WriteLine($"  Signal: {network.Strength}%");
        Console.WriteLine($"  Security: {network.WpaFlags}");
    }
    
    // Connect to a specific network
    try
    {
        await wifiDevice.ConnectAccessPoint("MyWiFiNetwork");
        Console.WriteLine("Connected successfully!");
    }
    catch (AccessPointNotFoundException)
    {
        Console.WriteLine("Network not found");
    }
    catch (ActivationFailedException ex)
    {
        Console.WriteLine($"Connection failed: {ex.Reason}");
    }
}
```

### Real-time Network Monitoring

```csharp
using ModelingEvolution.NetworkManager;

using var client = await NetworkManagerClient.Create();
var devices = await client.GetDevices();

// Monitor device state changes
foreach (var device in devices)
{
    await device.SubscribeStateChanged();
    device.StateChanged += (sender, args) =>
    {
        Console.WriteLine($"Device {device.InterfaceName}: {args.OldState} → {args.NewState}");
    };
}

// Monitor WiFi access point discovery
var wifiDevices = await client.GetWifiDevices();
foreach (var wifiDevice in wifiDevices)
{
    await wifiDevice.SubscribeAccessPoint(monitorSignal: true);
    
    wifiDevice.AccessPointVisilibityChanged += (sender, args) =>
    {
        Console.WriteLine($"Access Point {args.Path}: {args.Operation}");
    };
    
    wifiDevice.AccessPointSignalChanged += (sender, args) =>
    {
        Console.WriteLine($"Signal strength changed: {args.Ssid} → {args.Strength}%");
    };
}

// Keep monitoring
Console.WriteLine("Monitoring network changes... Press any key to stop.");
Console.ReadKey();
```

### Advanced Network Configuration

```csharp
using ModelingEvolution.NetworkManager;
using ModelingEvolution.Ipv4;

using var client = await NetworkManagerClient.Create();

// Create a static IP configuration
var staticConfig = new Dictionary<string, Dictionary<string, Variant>>
{
    ["connection"] = new()
    {
        ["id"] = Variant.String("Static Connection"),
        ["type"] = Variant.String("802-3-ethernet"),
        ["autoconnect"] = Variant.Bool(true)
    },
    ["ipv4"] = new()
    {
        ["method"] = Variant.String("manual"),
        ["addresses"] = Variant.Array(new[]
        {
            Variant.Array(new[]
            {
                Variant.UInt32(Ipv4Address.Parse("192.168.1.100").Value),
                Variant.UInt32(24),
                Variant.UInt32(Ipv4Address.Parse("192.168.1.1").Value)
            })
        }),
        ["dns"] = Variant.Array(new[]
        {
            Variant.UInt32(Ipv4Address.Parse("8.8.8.8").Value),
            Variant.UInt32(Ipv4Address.Parse("8.8.4.4").Value)
        })
    }
};

// Get ethernet device
var ethernetDevice = (await client.GetDevices())
    .FirstOrDefault(d => d.DeviceType == DeviceType.Ethernet);

if (ethernetDevice != null)
{
    // Create and activate the connection
    var (connectionPath, activeConnectionPath) = await client.NetworkManager
        .AddAndActivateConnectionAsync(staticConfig, ethernetDevice.Id, "/");
    
    Console.WriteLine($"Static connection activated: {activeConnectionPath}");
}
```

### Connection Profile Management

```csharp
using ModelingEvolution.NetworkManager;

using var client = await NetworkManagerClient.Create();

// List all saved connection profiles
var connections = await client.GetConnections();
Console.WriteLine("Saved Connections:");

foreach (var connection in connections)
{
    var profile = await connection.GetProfileInfo();
    if (profile != null)
    {
        Console.WriteLine($"  {profile.FileName}");
    }
}

// Get active connections
var activeConnections = await client.GetActiveConnections();
Console.WriteLine($"\nActive Connections: {activeConnections.Count}");

// Check if a specific connection is active
bool isVpnActive = await client.IsConnectionActive("My VPN");
Console.WriteLine($"VPN Active: {isVpnActive}");
```

## Advanced Usage

### Custom Connection Types

```csharp
// Create a bridge connection
var bridgeConfig = new Dictionary<string, Dictionary<string, Variant>>
{
    ["connection"] = new()
    {
        ["id"] = Variant.String("Bridge0"),
        ["type"] = Variant.String("bridge")
    },
    ["bridge"] = new()
    {
        ["stp"] = Variant.Bool(false)
    },
    ["ipv4"] = new()
    {
        ["method"] = Variant.String("auto")
    }
};

// Add bridge connection
var settings = client.Service.CreateSettings("/org/freedesktop/NetworkManager/Settings");
var bridgeConnectionPath = await settings.AddConnectionAsync(bridgeConfig);
```

### Device Information and Statistics

```csharp
var device = (await client.GetDevices()).First();

// Get comprehensive device information
var connectionInfo = await device.GetConnectionInfo();
if (connectionInfo != null)
{
    var config = connectionInfo.Ip4Config;
    Console.WriteLine($"IP Address: {config.Address}");
    Console.WriteLine($"Subnet: {config.Network}");
    Console.WriteLine($"Gateway: {config.Gateway}");
    Console.WriteLine($"Can reach 8.8.8.8 directly: {config.CanReachDirectly("8.8.8.8")}");
}

// Monitor connection profile changes
var profile = await device.GetConnectionProfile();
if (profile != null)
{
    Console.WriteLine($"Active Profile: {profile.FileName}");
}
```

### Error Handling and Diagnostics

```csharp
try
{
    using var client = await NetworkManagerClient.Create();
    var wifiDevice = (await client.GetWifiDevices()).FirstOrDefault();
    
    if (wifiDevice == null)
    {
        throw new InvalidOperationException("No WiFi device found");
    }
    
    await wifiDevice.ConnectAccessPoint("SecureNetwork", wait: true);
}
catch (AccessPointNotFoundException)
{
    Console.WriteLine("The specified network was not found in the scan results");
}
catch (ActivationFailedException ex)
{
    Console.WriteLine($"Connection failed: {ex.Reason}");
    Console.WriteLine($"Profile: {ex.ProfileFileName}");
    
    switch (ex.Reason)
    {
        case ActivationFailedReason.NoSecrets:
            Console.WriteLine("Authentication required - check credentials");
            break;
        case ActivationFailedReason.SsidNotFound:
            Console.WriteLine("Network out of range or hidden");
            break;
        case ActivationFailedReason.ConnectTimeout:
            Console.WriteLine("Connection timed out - check signal strength");
            break;
    }
}
catch (TimeoutException)
{
    Console.WriteLine("Operation timed out after 110 seconds");
}
```

## API Reference

### Core Classes

#### NetworkManagerClient
Main entry point for NetworkManager operations.

**Key Methods:**
- `Create()` - Creates and initializes a new client
- `GetDevices()` - Retrieve all network devices
- `GetWifiDevices()` - Get WiFi-capable devices
- `GetActiveConnections()` - List active connections
- `IsConnectionActive(string name)` - Check connection status

#### DeviceInfo
Represents a network device with comprehensive management capabilities.

**Key Properties:**
- `InterfaceName` - Network interface name (e.g., "wlan0", "eth0")
- `DeviceType` - Type of device (WiFi, Ethernet, Bridge, etc.)
- `State` - Current device state (Connected, Disconnected, etc.)

**Key Methods:**
- `GetConnectionInfo()` - Get current IP configuration
- `GetConnectionProfile()` - Get active connection profile
- `ActivateProfile(ProfileInfo)` - Activate a saved profile
- `DisconnectAsync()` - Disconnect the device
- `SubscribeStateChanged()` - Monitor state changes

#### WifiDeviceInfo
Extended device information for WiFi devices.

**Key Methods:**
- `ConnectAccessPoint(string ssid, bool wait = true)` - Connect to WiFi network
- `AccessPoint()` - Get current access point information
- `SubscribeAccessPoint(bool monitorSignal = false)` - Monitor access points

**Events:**
- `AccessPointVisibilityChanged` - Access point discovered/lost
- `AccessPointSignalChanged` - Signal strength changes

### Network Configuration

#### ConnectionInfo
Current network connection information.

**Properties:**
- `Ip4Config` - IPv4 configuration with address, network, gateway

#### Ipv4Configuration (from ModelingEvolution.Ipv4)
Comprehensive IPv4 network configuration.

**Key Methods:**
- `CanReachDirectly(string address)` - Check if address is in same network
- `IsValid()` - Validate configuration consistency

### Device Types

Supported NetworkManager device types:
- `Ethernet` - Wired network interfaces
- `Wifi` - Wireless network interfaces  
- `Bridge` - Network bridges
- `Bond` - Link aggregation
- `VLAN` - Virtual LANs
- `VPN` - VPN connections
- `Modem` - Mobile broadband
- `Bluetooth` - Bluetooth networking
- And many more...

### Connection States

Device and connection states:
- `Connected` - Fully connected and configured
- `Connecting` - Connection in progress
- `Disconnected` - Not connected
- `Unavailable` - Device not available
- `Failed` - Connection failed

## Requirements

- **.NET 9.0** or higher
- **Linux** operating system with NetworkManager
- **D-Bus** system bus access
- **ModelingEvolution.Ipv4** (automatically included)
- **Tmds.DBus.Protocol** (automatically included)

## Platform Support

This library is designed for Linux systems running NetworkManager:
- **Ubuntu** 18.04+
- **Debian** 10+
- **CentOS/RHEL** 8+
- **Fedora** 30+
- **Arch Linux**
- **openSUSE**

## Performance

The library is optimized for:
- **Minimal allocations** with efficient D-Bus communication
- **Async-first design** for responsive applications
- **Event-driven architecture** for real-time monitoring
- **Type safety** without runtime overhead
- **Batched operations** for bulk network management

## Deployment Considerations

### Permissions
Your application needs appropriate D-Bus permissions to access NetworkManager:

```xml
<!-- /etc/dbus-1/system.d/your-app.conf -->
<policy user="your-app-user">
  <allow send_destination="org.freedesktop.NetworkManager"/>
  <allow receive_sender="org.freedesktop.NetworkManager"/>
</policy>
```

### Service Configuration
For system services, ensure NetworkManager is running:

```bash
sudo systemctl enable NetworkManager
sudo systemctl start NetworkManager
```

### Security
- Always validate user input for network configurations
- Implement proper authentication for network management operations
- Use connection profiles for sensitive configurations
- Monitor for unauthorized network changes

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

This library extracts and enhances NetworkManager functionality from the [EventPi NetworkMonitor](https://github.com/modelingevolution/rocket-welder2) project, providing a focused, production-ready package for .NET NetworkManager integration.