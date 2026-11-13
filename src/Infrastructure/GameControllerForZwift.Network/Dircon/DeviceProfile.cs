using System.Text;

namespace GameControllerForZwift.Network.Dircon;

/// <summary>Shorthand presets combining an advertisement and a GATT tree (kept for the probe's --profile switch).</summary>
public enum DeviceProfileKind
{
    /// <summary>KICKR BIKE SHIFT as captured: Kickr advertisement, full GATT (default).</summary>
    Kickr,

    /// <summary>Ftms advertisement, no Wahoo services, no 0x42 gear frames.</summary>
    Ftms,

    /// <summary>Zwift advertisement, device information and the Zwift controller service only, no 0x42 gear frames.</summary>
    Zwift,
}

/// <summary>What the mDNS TXT record "ble-service-uuids" claims.</summary>
public enum AdvertKind
{
    /// <summary>Exact real-bike TXT: 0xFC82,0x1818,0x1826,Zwift 128-bit,A026EE0D-...</summary>
    Kickr,

    /// <summary>0x1826,0xFC82,Zwift 128-bit.</summary>
    Ftms,

    /// <summary>0xFC82,Zwift 128-bit.</summary>
    Zwift,
}

/// <summary>Which GATT tree is served over DIRCON.</summary>
public enum GattKind
{
    /// <summary>The full captured KICKR set (Wahoo A026xxxx services included).</summary>
    Full,

    /// <summary>The KICKR set without any A026xxxx service (no E037/E03C behaviour): 180A, 1818, 1826, 181C, Zwift.</summary>
    NoWahoo,

    /// <summary>180A and the Zwift service only.</summary>
    Minimal,

    /// <summary>
    /// BikeControl's tree as captured (docs/captures/BikeControl Loopback.pcapng): FC82 and 00000001-19CA (both with 0002/0003/0004),
    /// d273f680-..., 1826, 180A (BikeControl / 1337 / 1.0 / 1.0), 180D. FTMS and HR are served with idle values and never notified.
    /// </summary>
    BikeControlRide,
}

/// <summary>
/// What kind of Zwift device we present in the post-connect replies (0x3c device info, RideOn reply). Independent of the
/// mDNS advertisement, so the KICKR (Wahoo icon) advert can carry a controller identity. Probe-only; see
/// docs/zwift-lan-protocol-notes.md, "Controller identity (device-info) research".
/// </summary>
public enum ControllerIdentity
{
    /// <summary>KICKR BIKE SHIFT as captured: trainer + user-input capabilities, protocol 2.2 (default).</summary>
    KickrBike,

    /// <summary>Zwift Ride: user-input device only. Protocol version is NOT known (falls back to the KICKR value).</summary>
    Ride,

    /// <summary>Zwift Click v2: user-input device only, protocol 2.3.</summary>
    ClickV2,

    /// <summary>Zwift Click v1: user-input device only, protocol 1.3.</summary>
    ClickV1,

    /// <summary>Zwift Play: user-input device only, protocol 1.4.</summary>
    Play,

    /// <summary>
    /// BikeControl presenting as a "Zwift Ride" (probe --identity bcride): byte-exact 0x3c replies for ids 0, 16, 1024 and 771 from the
    /// loopback capture, "RideOn 02 00" then "RIDE_ON(0)" status, and no reply to 0x41, 0x44, id 1280 or anything else.
    /// </summary>
    BikeControlRide,
}

/// <summary>Independent advertisement / GATT / behaviour knobs.</summary>
/// <param name="Advert">TXT advertisement.</param>
/// <param name="Gatt">Served GATT tree.</param>
/// <param name="EnableWahooGearing">Send 0x42 gear frames and 0x3?? ride-data async frames (Zwift service). Independent of the GATT tree.</param>
/// <param name="Manufacturer">Value of 2A29 in every GATT kind.</param>
/// <param name="Controller">Identity presented in the 0x3c device-info and RideOn replies (default: the captured KICKR BIKE).</param>
public sealed record DeviceProfileOptions(
    AdvertKind Advert = AdvertKind.Kickr,
    GattKind Gatt = GattKind.Full,
    bool EnableWahooGearing = true,
    string Manufacturer = DeviceProfiles.DefaultManufacturer,
    ControllerIdentity Controller = ControllerIdentity.KickrBike,
    string? MfgData = null,
    IReadOnlyList<string>? TxtExtra = null,
    bool AddObcUuid = false,
    bool AddPowerService = false)
{
    /// <summary>kickr/full with gearing (the app default).</summary>
    public static DeviceProfileOptions Default { get; } = new();

    /// <summary>Maps a shorthand preset to options.</summary>
    public static DeviceProfileOptions FromKind(DeviceProfileKind kind) => kind switch
    {
        DeviceProfileKind.Ftms => new DeviceProfileOptions(AdvertKind.Ftms, GattKind.NoWahoo, false),
        DeviceProfileKind.Zwift => new DeviceProfileOptions(AdvertKind.Zwift, GattKind.Minimal, false),
        _ => new DeviceProfileOptions(),
    };

    /// <summary>OpenBikeControl BLE service UUID (appended to ble-service-uuids with --add-obc-uuid).</summary>
    public const string ObcServiceUuid = "d273f680-d548-419d-b9d1-fa0472345229";

    /// <summary>
    /// Builds the mDNS TXT manufacturer-data value (ASCII hex text): Zwift BLE company id 0x094A little-endian (4A09),
    /// a device-type byte (ride 08, clickv1 09, clickv2 0B, play 03) and two id bytes. "bikecontrol" is the literal capture value 4A0906B653.
    /// Returns null for an unknown preset.
    /// </summary>
    public static string? MfgPreset(string preset) => preset.ToLowerInvariant() switch
    {
        "bikecontrol" => "4A0906B653",
        "ride" => "4A0908B653",
        "clickv1" => "4A0909B653",
        "clickv2" => "4A090BB653",
        "play" => "4A0903B653",
        _ => null,
    };

    /// <summary>True when the Wahoo (A026xxxx) services are served and E03C notifications make sense.</summary>
    public bool HasWahooServices => Gatt == GattKind.Full;

    /// <summary>True for the BikeControl replica, which sends only the 5 s idle keypad frame (no ride data, no relayed FTMS/HR/power notifications).</summary>
    public bool IsBikeControlReplica => Gatt == GattKind.BikeControlRide;
}

/// <summary>Per-option GATT tree, mDNS service list and naming.</summary>
public static class DeviceProfiles
{
    /// <summary>Default manufacturer string (2A29).</summary>
    public const string DefaultManufacturer = "Wahoo Fitness";

    private const string ZwiftService128 = "00000001-19CA-4651-86E5-FA29DCDD09D1";

    /// <summary>
    /// The "bcride" preset (BikeControl replica; verified: Zwift treats it as a Zwift Ride): Kickr advert, BikeControl GATT tree,
    /// no Wahoo gearing frames, BikeControl manufacturer data and the OpenBikeControl UUID in the advert.
    /// </summary>
    public static DeviceProfileOptions ZwiftRidePreset() => new(
        AdvertKind.Kickr,
        GattKind.BikeControlRide,
        EnableWahooGearing: false,
        Controller: ControllerIdentity.BikeControlRide,
        MfgData: DeviceProfileOptions.MfgPreset("bikecontrol"),
        AddObcUuid: true);

    /// <summary>Default display name: the KICKR name only for kickr advert + full GATT, otherwise "Voltage Gamepad".</summary>
    public static string DefaultName(DeviceProfileOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options.Advert == AdvertKind.Kickr && options.Gatt == GattKind.Full ? "KICKR BIKE SHIFT GC4Z" : "Voltage Gamepad";
    }

    /// <summary>Value of the mDNS TXT "ble-service-uuids" entry.</summary>
    public static string BleServiceUuids(AdvertKind advert) => advert switch
    {
        AdvertKind.Ftms => "0x1826,0xFC82," + ZwiftService128,
        AdvertKind.Zwift => "0xFC82," + ZwiftService128,
        _ => "0xFC82,0x1818,0x1826," + ZwiftService128 + ",A026EE0D-0A7D-4AB3-97FA-F1500F9FEB8B",
    };

    /// <summary>Creates the served services for the options.</summary>
    public static IReadOnlyList<GattService> CreateServices(DeviceProfileOptions options, DeviceIdentity id)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(id);
        byte[] manufacturer = Encoding.ASCII.GetBytes(options.Manufacturer ?? DefaultManufacturer);

        if (options.Gatt == GattKind.BikeControlRide)
        {
            // The Wahoo default means "not overridden": BikeControl's own 2A29 value applies.
            string? custom = string.Equals(options.Manufacturer, DefaultManufacturer, StringComparison.Ordinal) ? null : options.Manufacturer;
            return BikeControlProfile.Create(custom, options.AddPowerService);
        }

        if (options.Gatt != GattKind.Minimal)
        {
            var all = KickrBikeProfile.Create(id);
            var list = new List<GattService>();
            foreach (var service in all)
            {
                if (options.Gatt == GattKind.NoWahoo && IsWahoo(service.Uuid))
                    continue;
                list.Add(service);
            }
            ApplyManufacturer(list, manufacturer);
            return list;
        }

        const GattProperties R = GattProperties.Read;
        var minimal = new List<GattService>
        {
            new(DirconUuids.DeviceInformation, new[]
            {
                new GattCharacteristic(DirconUuids.ManufacturerName, R, manufacturer),
                new GattCharacteristic(DirconUuids.SerialNumber, R, Encoding.ASCII.GetBytes(id.SerialNumber)),
                new GattCharacteristic(DirconUuids.HardwareRevision, R, Encoding.ASCII.GetBytes(id.HardwareRevision)),
                new GattCharacteristic(DirconUuids.FirmwareRevision, R, Encoding.ASCII.GetBytes(id.FirmwareVersion)),
            }),
            new(DirconUuids.ZwiftService, new[]
            {
                new GattCharacteristic(DirconUuids.ZwiftAsync, GattProperties.Notify),
                new GattCharacteristic(DirconUuids.ZwiftSyncRx, GattProperties.Write),
                new GattCharacteristic(DirconUuids.ZwiftSyncTx, GattProperties.Notify),
            }),
        };
        return minimal;
    }

    private static bool IsWahoo(BleUuid uuid) =>
        uuid == DirconUuids.WahooEE01 || uuid == DirconUuids.WahooEE0D || uuid == DirconUuids.WahooEE0B || uuid == DirconUuids.WahooEE06;

    private static void ApplyManufacturer(List<GattService> services, byte[] manufacturer)
    {
        foreach (var service in services)
            foreach (var characteristic in service.Characteristics)
                if (characteristic.Uuid == DirconUuids.ManufacturerName)
                    characteristic.Value = (byte[])manufacturer.Clone();
    }
}
