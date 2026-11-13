using System.Text;

namespace GameControllerForZwift.Network.Dircon;

/// <summary>GATT characteristic property bits as used in DIRCON characteristic discovery.</summary>
[Flags]
public enum GattProperties : byte
{
    None = 0,
    Read = 0x01,
    Write = 0x02,
    Notify = 0x04,
    /// <summary>DIRCON bit 3: indicate (BikeControl's 00000004 in the 00000001-19CA service, and FTMS 2AD9 as Write|Indicate).</summary>
    Indicate = 0x08,
}

/// <summary>A GATT characteristic with a static read value.</summary>
public sealed class GattCharacteristic
{
    public GattCharacteristic(BleUuid uuid, GattProperties properties, byte[]? value = null)
    {
        Uuid = uuid;
        Properties = properties;
        Value = value ?? Array.Empty<byte>();
    }

    public BleUuid Uuid { get; }
    public GattProperties Properties { get; }
    public byte[] Value { get; set; }
}

/// <summary>A GATT service and its characteristics (in discovery order).</summary>
public sealed class GattService
{
    public GattService(BleUuid uuid, IEnumerable<GattCharacteristic>? characteristics = null)
    {
        Uuid = uuid;
        Characteristics = characteristics?.ToList() ?? new List<GattCharacteristic>();
    }

    public BleUuid Uuid { get; }
    public IReadOnlyList<GattCharacteristic> Characteristics { get; }
}

/// <summary>Well-known UUIDs used by the emulated bike.</summary>
public static class DirconUuids
{
    public static readonly BleUuid DeviceInformation = BleUuid.FromShort(0x180A);
    public static readonly BleUuid ManufacturerName = BleUuid.FromShort(0x2A29);
    public static readonly BleUuid SerialNumber = BleUuid.FromShort(0x2A25);
    public static readonly BleUuid HardwareRevision = BleUuid.FromShort(0x2A27);
    public static readonly BleUuid FirmwareRevision = BleUuid.FromShort(0x2A26);

    public static readonly BleUuid CyclingPower = BleUuid.FromShort(0x1818);
    public static readonly BleUuid CyclingPowerMeasurement = BleUuid.FromShort(0x2A63);
    public static readonly BleUuid CyclingPowerFeature = BleUuid.FromShort(0x2A65);
    public static readonly BleUuid SensorLocation = BleUuid.FromShort(0x2A5D);
    public static readonly BleUuid CyclingPowerControlPoint = BleUuid.FromShort(0x2A66);

    public static readonly BleUuid FitnessMachine = BleUuid.FromShort(0x1826);
    public static readonly BleUuid FtmsFeature = BleUuid.FromShort(0x2ACC);
    public static readonly BleUuid FtmsControlPoint = BleUuid.FromShort(0x2AD9);
    public static readonly BleUuid IndoorBikeData = BleUuid.FromShort(0x2AD2);
    public static readonly BleUuid FtmsStatus = BleUuid.FromShort(0x2ADA);
    public static readonly BleUuid SupportedPowerRange = BleUuid.FromShort(0x2AD8);

    public static readonly BleUuid UserData = BleUuid.FromShort(0x181C);
    public static readonly BleUuid Weight = BleUuid.FromShort(0x2A98);

    public static readonly BleUuid WahooEE01 = BleUuid.Parse("A026EE01-0A7D-4AB3-97FA-F1500F9FEB8B");
    public static readonly BleUuid WahooEE0D = BleUuid.Parse("A026EE0D-0A7D-4AB3-97FA-F1500F9FEB8B");
    public static readonly BleUuid WahooEE0B = BleUuid.Parse("A026EE0B-0A7D-4AB3-97FA-F1500F9FEB8B");
    public static readonly BleUuid WahooEE06 = BleUuid.Parse("A026EE06-0A7D-4AB3-97FA-F1500F9FEB8B");
    public static readonly BleUuid WahooE039 = BleUuid.Parse("A026E039-0A7D-4AB3-97FA-F1500F9FEB8B");
    public static readonly BleUuid WahooE03A = BleUuid.Parse("A026E03A-0A7D-4AB3-97FA-F1500F9FEB8B");
    public static readonly BleUuid WahooE03C = BleUuid.Parse("A026E03C-0A7D-4AB3-97FA-F1500F9FEB8B");
    public static readonly BleUuid WahooE037 = BleUuid.Parse("A026E037-0A7D-4AB3-97FA-F1500F9FEB8B");

    public static readonly BleUuid ZwiftService = BleUuid.Parse("00000001-19CA-4651-86E5-FA29DCDD09D1");
    /// <summary>Zwift AsyncOut: notifications (keypad, ride data).</summary>
    public static readonly BleUuid ZwiftAsync = BleUuid.Parse("00000002-19CA-4651-86E5-FA29DCDD09D1");
    /// <summary>Zwift SyncRx: Zwift writes requests here.</summary>
    public static readonly BleUuid ZwiftSyncRx = BleUuid.Parse("00000003-19CA-4651-86E5-FA29DCDD09D1");
    /// <summary>Zwift SyncTx: replies to SyncRx requests.</summary>
    public static readonly BleUuid ZwiftSyncTx = BleUuid.Parse("00000004-19CA-4651-86E5-FA29DCDD09D1");
}

/// <summary>Builds the GATT tree of a KICKR BIKE SHIFT as enumerated in the real capture.</summary>
public static class KickrBikeProfile
{
    /// <summary>FTMS Feature value from capture.</summary>
    public static readonly byte[] FtmsFeatureValue = { 0x02, 0x40, 0x00, 0x00, 0x08, 0x60, 0x00, 0x00 };

    /// <summary>Idle Cycling Power Measurement (2A63) notification from capture.</summary>
    public static readonly byte[] IdlePowerMeasurement =
        Convert.FromHexString("3c000000e67eeb66000081494373eeed");

    /// <summary>Idle Indoor Bike Data (2AD2) notification from capture.</summary>
    public static readonly byte[] IdleIndoorBikeData = Convert.FromHexString("4400000000000000");

    /// <summary>Idle value of Wahoo characteristic E03C (1 Hz in capture).</summary>
    public static readonly byte[] IdleE03C = Convert.FromHexString("ff0f04120f09043f040c20070140");

    /// <summary>
    /// Creates the services in the discovery order seen in the capture. Read values not captured
    /// (2A65, 2A5D, 2AD8) are plausible placeholders; Zwift never read them.
    /// </summary>
    public static IReadOnlyList<GattService> Create(DeviceIdentity id)
    {
        ArgumentNullException.ThrowIfNull(id);
        const GattProperties R = GattProperties.Read;
        const GattProperties N = GattProperties.Notify;
        const GattProperties WN = GattProperties.Write | GattProperties.Notify;
        const GattProperties RW = GattProperties.Read | GattProperties.Write;

        return new List<GattService>
        {
            new(DirconUuids.DeviceInformation, new[]
            {
                new GattCharacteristic(DirconUuids.ManufacturerName, R, Encoding.ASCII.GetBytes("Wahoo Fitness")),
                new GattCharacteristic(DirconUuids.SerialNumber, R, Encoding.ASCII.GetBytes(id.SerialNumber)),
                new GattCharacteristic(DirconUuids.HardwareRevision, R, Encoding.ASCII.GetBytes(id.HardwareRevision)),
                new GattCharacteristic(DirconUuids.FirmwareRevision, R, Encoding.ASCII.GetBytes(id.FirmwareVersion)),
            }),
            // Characteristics of EE01 were not enumerated by Zwift in the capture.
            new(DirconUuids.WahooEE01),
            new(DirconUuids.CyclingPower, new[]
            {
                new GattCharacteristic(DirconUuids.CyclingPowerMeasurement, N),
                new GattCharacteristic(DirconUuids.CyclingPowerFeature, R, new byte[] { 0, 0, 0, 0 }),
                new GattCharacteristic(DirconUuids.SensorLocation, R, new byte[] { 0x0D }),
                new GattCharacteristic(DirconUuids.CyclingPowerControlPoint, WN),
            }),
            new(DirconUuids.FitnessMachine, new[]
            {
                new GattCharacteristic(DirconUuids.FtmsFeature, R, (byte[])FtmsFeatureValue.Clone()),
                new GattCharacteristic(DirconUuids.FtmsControlPoint, WN),
                new GattCharacteristic(DirconUuids.IndoorBikeData, N),
                new GattCharacteristic(DirconUuids.FtmsStatus, N),
                new GattCharacteristic(DirconUuids.SupportedPowerRange, R, new byte[] { 0x00, 0x00, 0xD0, 0x07, 0x01, 0x00 }),
            }),
            new(DirconUuids.UserData, new[]
            {
                new GattCharacteristic(DirconUuids.Weight, RW, new byte[] { 0x02, 0x3E }),
            }),
            new(DirconUuids.WahooEE0D, new[]
            {
                new GattCharacteristic(DirconUuids.WahooE039, WN),
                new GattCharacteristic(DirconUuids.WahooE03A, N),
                new GattCharacteristic(DirconUuids.WahooE03C, N),
            }),
            new(DirconUuids.WahooEE0B, new[]
            {
                new GattCharacteristic(DirconUuids.WahooE037, WN),
            }),
            // Characteristics of EE06 were not enumerated by Zwift in the capture.
            new(DirconUuids.WahooEE06),
            new(DirconUuids.ZwiftService, new[]
            {
                new GattCharacteristic(DirconUuids.ZwiftAsync, N),
                new GattCharacteristic(DirconUuids.ZwiftSyncRx, GattProperties.Write),
                new GattCharacteristic(DirconUuids.ZwiftSyncTx, N),
            }),
        };
    }
}

/// <summary>
/// The GATT tree BikeControl presents as a "Zwift Ride", decoded from docs/captures/BikeControl Loopback.pcapng (Zwift on TCP 36868):
/// services FC82, OpenBikeControl d273f680-..., 1826, 180A, 00000001-19CA, 180D in that order. The Zwift characteristics appear
/// twice (FC82 and 00000001-19CA); DIRCON addresses characteristics by UUID only, so a session cannot tell the instances apart.
/// Properties are the DIRCON bits from the discovery replies. FTMS / HR are served with static idle values (BikeControl relays a
/// real trainer there; we never send relayed notifications).
/// </summary>
public static class BikeControlProfile
{
    /// <summary>Manufacturer name (2A29) of BikeControl.</summary>
    public const string Manufacturer = "BikeControl";

    /// <summary>Serial number (2A25) of BikeControl.</summary>
    public const string Serial = "1337";

    /// <summary>Hardware and firmware revision (2A27, 2A26) of BikeControl.</summary>
    public const string Revision = "1.0";

    /// <summary>Zwift Controls service in its 16-bit form (0xFC82).</summary>
    public static readonly BleUuid ZwiftShortService = BleUuid.FromShort(0xFC82);

    /// <summary>OpenBikeControl service, listed second; Zwift never enumerated its characteristics, so it is served empty.</summary>
    public static readonly BleUuid ObcService = BleUuid.Parse(DeviceProfileOptions.ObcServiceUuid);

    /// <summary>FTMS Feature value (2ACC) read by Zwift from BikeControl: 8a 40 00 00 1e 20 00 00.</summary>
    public static readonly byte[] FtmsFeatureValue = { 0x8A, 0x40, 0x00, 0x00, 0x1E, 0x20, 0x00, 0x00 };

    private static readonly BleUuid HeartRateService = BleUuid.FromShort(0x180D);
    private static readonly BleUuid HeartRateMeasurement = BleUuid.FromShort(0x2A37);
    private static readonly BleUuid BodySensorLocation = BleUuid.FromShort(0x2A38);
    private static readonly BleUuid FtmsTrainingStatus = BleUuid.FromShort(0x2AD3);
    private static readonly BleUuid FtmsSupportedInclination = BleUuid.FromShort(0x2AD5);
    private static readonly BleUuid FtmsSupportedResistance = BleUuid.FromShort(0x2AD6);

    /// <summary>Creates the services in capture order. <paramref name="manufacturer"/> overrides 2A29 (default "BikeControl").</summary>
    public static IReadOnlyList<GattService> Create(string? manufacturer = null, bool addPowerService = false)
    {
        const GattProperties R = GattProperties.Read;
        const GattProperties N = GattProperties.Notify;
        const GattProperties W = GattProperties.Write;
        const GattProperties I = GattProperties.Indicate;

        var services = new List<GattService>
        {
            new(ZwiftShortService, new[]
            {
                new GattCharacteristic(DirconUuids.ZwiftSyncRx, W),
                new GattCharacteristic(DirconUuids.ZwiftAsync, N),
                new GattCharacteristic(DirconUuids.ZwiftSyncTx, N),
            }),
            new(ObcService),
            new(DirconUuids.FitnessMachine, new[]
            {
                new GattCharacteristic(DirconUuids.FtmsFeature, R, (byte[])FtmsFeatureValue.Clone()),
                new GattCharacteristic(DirconUuids.IndoorBikeData, N),
                new GattCharacteristic(FtmsSupportedInclination, R),
                new GattCharacteristic(FtmsSupportedResistance, R),
                new GattCharacteristic(DirconUuids.SupportedPowerRange, R, new byte[] { 0x00, 0x00, 0xD0, 0x07, 0x01, 0x00 }),
                new GattCharacteristic(DirconUuids.FtmsControlPoint, W | I),
                new GattCharacteristic(DirconUuids.FtmsStatus, N),
                new GattCharacteristic(FtmsTrainingStatus, R | N),
            }),
            new(DirconUuids.DeviceInformation, new[]
            {
                new GattCharacteristic(DirconUuids.ManufacturerName, R, Encoding.ASCII.GetBytes(manufacturer ?? Manufacturer)),
                new GattCharacteristic(DirconUuids.SerialNumber, R, Encoding.ASCII.GetBytes(Serial)),
                new GattCharacteristic(DirconUuids.HardwareRevision, R, Encoding.ASCII.GetBytes(Revision)),
                new GattCharacteristic(DirconUuids.FirmwareRevision, R, Encoding.ASCII.GetBytes(Revision)),
            }),
            new(DirconUuids.ZwiftService, new[]
            {
                new GattCharacteristic(DirconUuids.ZwiftAsync, N),
                new GattCharacteristic(DirconUuids.ZwiftSyncRx, W),
                new GattCharacteristic(DirconUuids.ZwiftSyncTx, I),
            }),
            new(HeartRateService, new[]
            {
                new GattCharacteristic(HeartRateMeasurement, N),
                new GattCharacteristic(BodySensorLocation, R),
            }),
        };

        if (addPowerService)
        {
            // Opt-in (probe --add-power-service), not in BikeControl's tree: Cycling Power right after FTMS so Zwift can pick the probe as a power source via 2A63.
            services.Insert(3, new GattService(DirconUuids.CyclingPower, new[]
            {
                new GattCharacteristic(DirconUuids.CyclingPowerMeasurement, N),
                new GattCharacteristic(DirconUuids.CyclingPowerFeature, R, new byte[] { 0x0C, 0x01, 0x00, 0x00 }), // wheel rev, crank rev, accumulated torque
            }));
        }

        return services;
    }
}
