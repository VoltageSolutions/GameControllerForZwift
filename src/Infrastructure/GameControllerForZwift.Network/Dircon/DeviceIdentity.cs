namespace GameControllerForZwift.Network.Dircon;

/// <summary>Identity presented to Zwift (mDNS TXT record, device-info characteristics, Zwift device-info reply).</summary>
/// <param name="Name">Display / instance name.</param>
/// <param name="SerialNumber">Digits-only serial (9 digits; clients parse it as uint64).</param>
/// <param name="MacAddress">MAC in "02-47-5A-00-00-01" format.</param>
/// <param name="FirmwareVersion">Firmware revision string (2A26).</param>
/// <param name="HardwareRevision">Hardware revision string (2A27).</param>
public sealed record DeviceIdentity(
    string Name,
    string SerialNumber,
    string MacAddress,
    string FirmwareVersion,
    string HardwareRevision)
{
    /// <summary>Default identity.</summary>
    public static DeviceIdentity Default { get; } = new(
        "Game Controller for Zwift",
        "612034871",
        "02-47-5A-00-00-01",
        "1.21.18",
        "3");
}
