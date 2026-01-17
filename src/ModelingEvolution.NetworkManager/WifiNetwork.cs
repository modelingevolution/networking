using System.Text;
using System.Linq;

using ModelingEvolution.NetworkManager;
using Tmds.DBus.Protocol;
using Connection = ModelingEvolution.NetworkManager.Connection;

namespace ModelingEvolution.NetworkManager;

public record AccessPointInfo
{
        
    public string SourceInterface { get; init; }
    internal string DevicePath { get; init; }
    internal string AccessPointPath { get; init; }
    public string Ssid { get; init; }
    public byte SignalStrength { get; init; }
    public WifiAccessPointMode AccessPointMode { get; init; }
    public uint MaxKbitRate { get; init; }
    public DeviceInfo SourceDevice { get; set; }
    internal NetworkManagerClient Client { get; init; }
    public async Task<ProfileInfo> Connect(string password)
    {
        var connectionSettings = new Dictionary<string, Dictionary<string, VariantValue>>
        {
            {
                "802-11-wireless", new Dictionary<string, VariantValue>
                {
                    { "ssid", Ssid },
                    { "mode", "infrastructure" },
                    { "security", "802-11-wireless-security" }
                }
            },
            {
                "802-11-wireless-security", new Dictionary<string, VariantValue>
                {
                    { "psk", password },
                    { "auth-alg", "shared" },
                    { "key-mgmt", "wpa-psk" },
                    { "psk-flags", 0u } // none, system is responsible for storing pwd
                }
            },
            {
                "connection", new Dictionary<string, VariantValue>
                {
                    { "type", "802-11-wireless" },
                    { "id", Ssid }
                }
            },
        };
        var result = await Client.NetworkManager.AddAndActivateConnectionAsync(connectionSettings, DevicePath, AccessPointPath);
        var connection = Client.Service.CreateConnection(result.Path);
        return new ProfileInfo()
        {
            Id = result.Path,
            FileName =  await connection.GetFilenameAsync(),
            Client = Client
        };
    }
    
    internal Wireless Wireless
    {
        get => Client.Service.CreateWireless(this.DevicePath);
    }
    public async Task RequestScan(bool wait = true)
    {
        await Wireless.RequestScanAsync(new Dictionary<string, VariantValue>());
        if (wait) await Task.Delay(5000);
    }


    public async Task<ProfileInfo> UpdatePwd(string pwd, string dataFileName, string connectionId)
    {
        var profile = await Client.GetProfiles().FirstOrDefaultAsync(x => x.FileName == dataFileName);
        if (profile != null)
        {
            var connectionSettings = new Dictionary<string, Dictionary<string, VariantValue>>
            {
                {
                    "802-11-wireless", new Dictionary<string, VariantValue>
                    {
                        { "ssid", VariantValue.Array(Encoding.UTF8.GetBytes(Ssid)) },
                    }
                },
                {
                    "802-11-wireless-security", new Dictionary<string, VariantValue>
                    {
                        { "psk", pwd },
                        { "key-mgmt", "wpa-psk" },
                        { "psk-flags", 0u } // none, system is responsible for storing pwd
                    }
                },
                {
                    "connection", new Dictionary<string, VariantValue>
                    {
                        { "id", connectionId ?? Ssid },
                        { "type", "802-11-wireless" },
                        { "interface-name", this.SourceInterface },
                    }
                },
            };
            // we should update the profile.
            await profile.Connection.UpdateAsync(connectionSettings);
            return profile;
        }
        else throw new ProfileNotFoundException();
    }
    public async Task<ProfileInfo> New(string pwd, string conName)
    {
        var connectionSettings = new Dictionary<string, Dictionary<string, VariantValue>>
        {
            {
                "802-11-wireless", new Dictionary<string, VariantValue>
                {
                    { "ssid", VariantValue.Array(Encoding.UTF8.GetBytes(Ssid)) },
                }
            },
            {
                "802-11-wireless-security", new Dictionary<string, VariantValue>
                {
                    { "psk", pwd },
                    { "key-mgmt", "wpa-psk" },
                    { "psk-flags", 0u } // none, system is responsible for storing pwd
                }
            },
            {
                "connection", new Dictionary<string, VariantValue>
                {
                    { "type", "802-11-wireless" },
                    { "id", conName ?? Ssid },
                    { "interface-name", this.SourceInterface },
                }
            },
        };

        var r = await Client.Settings.AddConnectionAsync(connectionSettings);
        var connection = Client.Service.CreateConnection(r);
        
        return new ProfileInfo()
        {
            Id = r,

            FileName = await connection.GetFilenameAsync(),
            Client = Client
        };
    }
}

public record WifiProfileSettings : ProfileSettings
{
    public string Ssid { get; init; }
    public string Mode { get; init; }
    
}
public record ProfileSettings
{
    public string? InterfaceName { get; init; }
    public string? ProfileName { get; set; }
}
public record ProfileInfo
{
    internal Connection Connection => Client.Service.CreateConnection(Id.Path);
    public string? FileName { get; init; }
    public Guid FileId => (FileName ?? Id.Path).ToGuid();
    public PathId Id { get; set; }
    public async Task<ProfileSettings?> Settings()
    {
        var props = await Connection.GetSettingsAsync();
        string interfaceName = null;
        string name = null;
        
        if (props.TryGetValue("connection", out var connectionInfo))
        {
            if (connectionInfo.TryGetValue("interface-name", out var v))
                interfaceName = v.GetString();
            if (connectionInfo.TryGetValue("id", out var id))
                name = id.GetString();
        }

        if (props.TryGetValue("802-11-wireless", out var wifiSettings))
        {
            //var secrets = await Connection.GetSecretsAsync(null);

            var ssid = wifiSettings.TryGetValue("ssid", out var r) ? Encoding.UTF8.GetString(r.GetArray<byte>()) : null;
            var mode = wifiSettings.TryGetValue("mode", out var m) ? m.GetString() : null;

            return new WifiProfileSettings()
            {
                InterfaceName = interfaceName,
                ProfileName = name,
                Mode = mode,
                Ssid = ssid
            };
        }
        
        return new ProfileSettings()
        {
            InterfaceName = interfaceName,
            ProfileName = name
        };
    }
    internal NetworkManagerClient Client { get; init; }
    

    public async Task Delete()
    {
        await Connection.DeleteAsync();
    }

    public async Task UpdatePwd(string pwd)
    {

    }

    public async Task<Dictionary<string, Dictionary<string, VariantValue>>> GetConnectionSettings()
    {
        return await Connection.GetSettingsAsync();
    }

    public async Task UpdateConnectionSettings(Dictionary<string, Dictionary<string, VariantValue>> settings)
    {
        await Connection.UpdateAsync(settings);
    }
    public async Task Deactivate(string? interfaceName = null)
    {
        var activeProfiles = await Client.GetActiveConnections();
        if (activeProfiles.Contains(this.FileId))
        {
            if (interfaceName == null)
            {
                var settings = await Settings();
                interfaceName = settings.InterfaceName;
            }

            var dev = await Client.GetDevices().FirstOrDefaultAsync(x => x.InterfaceName == interfaceName);
            await dev.DisconnectAsync();
        }
    }
    public async Task Activate(string? interfaceName =null)
    {
        if (interfaceName == null)
        {
            var settings = await Settings();
            interfaceName = settings.InterfaceName;
        }

        var dev = interfaceName != null
            ? await Client.GetDevices().FirstOrDefaultAsync(x => x.InterfaceName == interfaceName)
            : await Client.GetDevices().FirstOrDefaultAsync(x => x.DeviceType == DeviceType.Wifi);

        await dev.ActivateProfile(this);
    }
}