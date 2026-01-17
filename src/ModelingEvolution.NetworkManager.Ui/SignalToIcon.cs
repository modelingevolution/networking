using System.ComponentModel.DataAnnotations;
using ModelingEvolution.NetworkManager.Contracts;
using MicroPlumberd.Encryption;
using MudBlazor;

namespace ModelingEvolution.NetworkManager.Ui;

internal static class SignalToIcon
{
    public static string Convert(byte signal) =>
        signal switch
        {
            0 => Icons.Material.Filled.SignalWifiOff,
            < 15 => Icons.Material.Filled.SignalWifi0Bar,
            < 40 => Icons.Material.Filled.NetworkWifi1Bar,
            < 70 => Icons.Material.Filled.NetworkWifi2Bar,
            < 90 => Icons.Material.Filled.NetworkWifi3Bar,
            _ => Icons.Material.Filled.SignalWifi4Bar
        };
}

class DefineWirelessProfileSurrogate
{
    public string? FileName { get; set; }
    public string? InterfaceName { get; set; }

    [Required(AllowEmptyStrings = false)]
    [MinLength(1)]
    public string? Ssid { get; set; }

    [Required(AllowEmptyStrings = false)]
    [Length(8, 64)]
    public string? Password { get; set; }

    public DefineWirelessProfile Command(string recipient)
    {
        return new DefineWirelessProfile
        {
            FileName = FileName,
            InterfaceName = InterfaceName,
            Password = SecretObject<string>.Create(Password!, recipient),
            Ssid = Ssid
        };
    }
}
