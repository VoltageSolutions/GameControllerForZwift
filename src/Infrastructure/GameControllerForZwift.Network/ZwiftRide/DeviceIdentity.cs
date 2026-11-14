namespace GameControllerForZwift.Network.ZwiftRide;

/// <summary>Identity advertised over mDNS (instance name and the TXT "mac-address" / "serial-number" entries).</summary>
/// <param name="Name">Display / instance name.</param>
/// <param name="SerialNumber">Digits-only serial (clients parse it as uint64).</param>
/// <param name="MacAddress">MAC in "02-47-5A-00-00-01" format.</param>
public sealed record DeviceIdentity(string Name, string SerialNumber, string MacAddress)
{
    /// <summary>Default identity.</summary>
    public static DeviceIdentity Default { get; } = new("Game Controller for Zwift", "612034871", "02-47-5A-00-00-01");
}
