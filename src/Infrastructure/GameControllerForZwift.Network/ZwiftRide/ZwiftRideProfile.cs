using System.Text;
using GameControllerForZwift.Network.DirectConnect;

namespace GameControllerForZwift.Network.ZwiftRide;

/// <summary>
/// The one device presentation this application offers to Zwift: a Zwift Ride controller. It is the combination that was verified
/// to work (Zwift pairs it as a Zwift Ride): a Wahoo KICKR style mDNS advertisement, the GATT tree of the BikeControl app
/// (decoded from docs/captures/BikeControl Loopback.pcapng), BikeControl's manufacturer data and the OpenBikeControl service UUID.
/// Nothing here is configurable on purpose; see docs/zwift-lan-protocol-notes.md for how each piece was found.
/// </summary>
public static class ZwiftRideProfile
{
    /// <summary>OpenBikeControl BLE service UUID, listed in the advertisement and served as an empty service.</summary>
    public const string ObcServiceUuid = "d273f680-d548-419d-b9d1-fa0472345229";

    private const string ZwiftService128 = "00000001-19CA-4651-86E5-FA29DCDD09D1";

    /// <summary>
    /// Value of the mDNS TXT entry "ble-service-uuids": the services a real KICKR BIKE announces (0xFC82, 0x1818, 0x1826, Zwift
    /// 128-bit, Wahoo A026EE0D) followed by the OpenBikeControl UUID.
    /// </summary>
    public const string AdvertisedBleServiceUuids =
        "0xFC82,0x1818,0x1826," + ZwiftService128 + ",A026EE0D-0A7D-4AB3-97FA-F1500F9FEB8B," + ObcServiceUuid;

    /// <summary>Value of the mDNS TXT entry "manufacturer-data": Zwift's BLE company id (0x094A, little endian) plus BikeControl's device bytes.</summary>
    public const string ManufacturerData = "4A0906B653";

    /// <summary>Manufacturer name (2A29) of the served Device Information service.</summary>
    public const string Manufacturer = "BikeControl";

    /// <summary>Serial number (2A25) of the served Device Information service.</summary>
    public const string Serial = "1337";

    /// <summary>Hardware and firmware revision (2A27, 2A26) of the served Device Information service.</summary>
    public const string Revision = "1.0";

    /// <summary>Zwift Controls service in its 16-bit form (0xFC82).</summary>
    public static readonly BleUuid ZwiftShortService = BleUuid.FromShort(0xFC82);

    /// <summary>OpenBikeControl service; Zwift never enumerated its characteristics, so it is served empty.</summary>
    public static readonly BleUuid ObcService = BleUuid.Parse(ObcServiceUuid);

    /// <summary>FTMS Feature value (2ACC) read by Zwift: 8a 40 00 00 1e 20 00 00.</summary>
    public static readonly byte[] FtmsFeatureValue = { 0x8A, 0x40, 0x00, 0x00, 0x1E, 0x20, 0x00, 0x00 };

    private static readonly BleUuid HeartRateService = BleUuid.FromShort(0x180D);
    private static readonly BleUuid HeartRateMeasurement = BleUuid.FromShort(0x2A37);
    private static readonly BleUuid BodySensorLocation = BleUuid.FromShort(0x2A38);
    private static readonly BleUuid FtmsTrainingStatus = BleUuid.FromShort(0x2AD3);
    private static readonly BleUuid FtmsSupportedInclination = BleUuid.FromShort(0x2AD5);
    private static readonly BleUuid FtmsSupportedResistance = BleUuid.FromShort(0x2AD6);

    /// <summary>
    /// Creates the served GATT services in capture order: FC82, OpenBikeControl, 1826 (FTMS), 180A (Device Information), 00000001-19CA, 180D.
    /// The Zwift characteristics appear twice (FC82 and 00000001-19CA); DIRCON addresses characteristics by UUID only, so a session cannot
    /// tell the instances apart. FTMS and heart rate are static idle values and are never notified.
    /// </summary>
    public static IReadOnlyList<GattService> CreateServices()
    {
        const GattProperties R = GattProperties.Read;
        const GattProperties N = GattProperties.Notify;
        const GattProperties W = GattProperties.Write;
        const GattProperties I = GattProperties.Indicate;

        return new List<GattService>
        {
            new(ZwiftShortService, new[]
            {
                new GattCharacteristic(ZwiftRideUuids.SyncRx, W),
                new GattCharacteristic(ZwiftRideUuids.AsyncOut, N),
                new GattCharacteristic(ZwiftRideUuids.SyncTx, N),
            }),
            new(ObcService),
            new(ZwiftRideUuids.FitnessMachine, new[]
            {
                new GattCharacteristic(ZwiftRideUuids.FtmsFeature, R, (byte[])FtmsFeatureValue.Clone()),
                new GattCharacteristic(ZwiftRideUuids.IndoorBikeData, N),
                new GattCharacteristic(FtmsSupportedInclination, R),
                new GattCharacteristic(FtmsSupportedResistance, R),
                new GattCharacteristic(ZwiftRideUuids.SupportedPowerRange, R, new byte[] { 0x00, 0x00, 0xD0, 0x07, 0x01, 0x00 }),
                new GattCharacteristic(ZwiftRideUuids.FtmsControlPoint, W | I),
                new GattCharacteristic(ZwiftRideUuids.FtmsStatus, N),
                new GattCharacteristic(FtmsTrainingStatus, R | N),
            }),
            new(ZwiftRideUuids.DeviceInformation, new[]
            {
                new GattCharacteristic(ZwiftRideUuids.ManufacturerName, R, Encoding.ASCII.GetBytes(Manufacturer)),
                new GattCharacteristic(ZwiftRideUuids.SerialNumber, R, Encoding.ASCII.GetBytes(Serial)),
                new GattCharacteristic(ZwiftRideUuids.HardwareRevision, R, Encoding.ASCII.GetBytes(Revision)),
                new GattCharacteristic(ZwiftRideUuids.FirmwareRevision, R, Encoding.ASCII.GetBytes(Revision)),
            }),
            new(ZwiftRideUuids.ZwiftService, new[]
            {
                new GattCharacteristic(ZwiftRideUuids.AsyncOut, N),
                new GattCharacteristic(ZwiftRideUuids.SyncRx, W),
                new GattCharacteristic(ZwiftRideUuids.SyncTx, I),
            }),
            new(HeartRateService, new[]
            {
                new GattCharacteristic(HeartRateMeasurement, N),
                new GattCharacteristic(BodySensorLocation, R),
            }),
        };
    }
}

/// <summary>UUIDs of the services and characteristics served by <see cref="ZwiftRideProfile"/> that the code addresses directly.</summary>
public static class ZwiftRideUuids
{
    /// <summary>Device Information service (0x180A).</summary>
    public static readonly BleUuid DeviceInformation = BleUuid.FromShort(0x180A);
    /// <summary>Manufacturer Name String (0x2A29).</summary>
    public static readonly BleUuid ManufacturerName = BleUuid.FromShort(0x2A29);
    /// <summary>Serial Number String (0x2A25).</summary>
    public static readonly BleUuid SerialNumber = BleUuid.FromShort(0x2A25);
    /// <summary>Hardware Revision String (0x2A27).</summary>
    public static readonly BleUuid HardwareRevision = BleUuid.FromShort(0x2A27);
    /// <summary>Firmware Revision String (0x2A26).</summary>
    public static readonly BleUuid FirmwareRevision = BleUuid.FromShort(0x2A26);

    /// <summary>Fitness Machine service (0x1826).</summary>
    public static readonly BleUuid FitnessMachine = BleUuid.FromShort(0x1826);
    /// <summary>FTMS Feature (0x2ACC).</summary>
    public static readonly BleUuid FtmsFeature = BleUuid.FromShort(0x2ACC);
    /// <summary>FTMS Control Point (0x2AD9).</summary>
    public static readonly BleUuid FtmsControlPoint = BleUuid.FromShort(0x2AD9);
    /// <summary>Indoor Bike Data (0x2AD2).</summary>
    public static readonly BleUuid IndoorBikeData = BleUuid.FromShort(0x2AD2);
    /// <summary>Fitness Machine Status (0x2ADA).</summary>
    public static readonly BleUuid FtmsStatus = BleUuid.FromShort(0x2ADA);
    /// <summary>Supported Power Range (0x2AD8).</summary>
    public static readonly BleUuid SupportedPowerRange = BleUuid.FromShort(0x2AD8);

    /// <summary>Zwift controller service (00000001-19CA-...).</summary>
    public static readonly BleUuid ZwiftService = BleUuid.Parse("00000001-19CA-4651-86E5-FA29DCDD09D1");
    /// <summary>Zwift AsyncOut: notifications (keypad frames, status strings).</summary>
    public static readonly BleUuid AsyncOut = BleUuid.Parse("00000002-19CA-4651-86E5-FA29DCDD09D1");
    /// <summary>Zwift SyncRx: Zwift writes requests here.</summary>
    public static readonly BleUuid SyncRx = BleUuid.Parse("00000003-19CA-4651-86E5-FA29DCDD09D1");
    /// <summary>Zwift SyncTx: replies to SyncRx requests.</summary>
    public static readonly BleUuid SyncTx = BleUuid.Parse("00000004-19CA-4651-86E5-FA29DCDD09D1");
}
