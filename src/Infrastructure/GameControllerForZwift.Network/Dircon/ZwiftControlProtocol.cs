using System.Text;
using Microsoft.Extensions.Logging;

namespace GameControllerForZwift.Network.Dircon;

/// <summary>
/// Pure Zwift controller protocol logic (no sockets). Replays the bike's behaviour decoded from the capture.
/// One instance per connection (tracks the handshake state).
/// </summary>
public sealed class ZwiftControlProtocol
{
    private static readonly byte[] RideOnPrefix = Encoding.ASCII.GetBytes("RideOn");
    private static readonly byte[] RideOnStatusText = Convert.FromHexString("2a0803120d220b524944455f4f4e28332900"); // "RIDE_ON(3)"
    private static readonly byte[] BikeControlRideOnStatusText = Convert.FromHexString("2a0803120d220b524944455f4f4e28302900"); // "RIDE_ON(0)\0"

    // BikeControl ("Zwift Ride") replies decoded from docs/captures/BikeControl Loopback.pcapng; everything else it leaves unanswered.
    private static readonly Dictionary<uint, byte[]> BikeControlGetResponses = new()
    {
        // DevInfoPage: protocol 515, fw 00 00 01 01, name "Zwift Ride", serial "0B-58D15ABB4363", hw "B.0", caps {2,1}, f9=1, f10=8, uid "58D15ABB4363".
        [0] = Convert.FromHexString("3c080012450a430883041204000001011a0a5a776966742052696465320f30422d3538443135414242343336333a03422e30420408021001480150085a0c353844313541424234333633"),
        [16] = Convert.FromHexString("3c081012020800"),
        [1024] = Convert.FromHexString("3c088008120608ffff031003"),
        [771] = Convert.FromHexString("3c08830612080800106418002000"),
    };

    private static readonly byte[] AtxStatus0 = Convert.FromHexString("2a08031211220f4154582030382c2053545820303000");
    private static readonly byte[] AtxStatus1 = Convert.FromHexString("2a08031211220f4154582030382c2053545820303800");
    private static readonly byte[] E037Reply = Convert.FromHexString("fe33012000000020000000");
    private static readonly byte[] Reply3e = { 0x3E, 0x08, 0x41, 0x10, 0x02 };

    // StatusResponse { command = 0x44 (SENSOR_RELAY_CONFIG), status = 0 (SUCCESS) }; the Victory capture answers both `44 20 01` and `44 08 01` with it.
    private static readonly byte[] Reply3e44 = { 0x3E, 0x08, 0x44, 0x10, 0x00 };

    // Victory capture: `00 08 80 0a` (SENSOR_RELAY_DATA_PAGE, id 1280) -> supportedSensors {5, 2, 6, 7} (HRM, user input, power meter, cadence).
    private static readonly byte[] SensorRelayPageReply = Convert.FromHexString("3c08800a12060a0405020607");

    /// <summary>Zp.DeviceType values (zp.proto) used in DevInfoPage.deviceCapabilities.</summary>
    private const uint DeviceTypeCyclingTurboTrainer = 1;
    private const uint DeviceTypeUserInputDevice = 2;

    // Captured 0x3c replies keyed by the request id varint (bytes after "00 08").
    private static readonly Dictionary<uint, byte[]> CannedGetResponses = new()
    {
        [1024] = Convert.FromHexString("3c088008121d08d80110031a0a080110ffff0318feff031a0a080210ffff0318feff03"),
        [771] = Convert.FromHexString("3c088306"),
        [528] = Convert.FromHexString("3c089004120408012003"),
    };

    private readonly DeviceIdentity _identity;
    private readonly ILogger? _logger;
    private readonly ControllerIdentity _controller;
    private readonly ushort _protocolVersion;
    private readonly (uint DeviceType, uint Capabilities)[] _capabilities;
    private readonly byte[] _rideOnReply;
    private readonly Func<int?>? _batteryPercent;

    /// <summary>Creates the protocol handler.</summary>
    /// <param name="identity">Name / serial / firmware strings.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="controller">Identity presented in the 0x3c device-info and RideOn replies (default: captured KICKR BIKE).</param>
    /// <param name="batteryPercent">Optional provider of the battery percentage (0..100) reported in the BikeControlRide id-771 reply; null result keeps the captured 100.</param>
    public ZwiftControlProtocol(DeviceIdentity identity, ILogger? logger = null, ControllerIdentity controller = ControllerIdentity.KickrBike, Func<int?>? batteryPercent = null)
    {
        _batteryPercent = batteryPercent;
        _identity = identity ?? throw new ArgumentNullException(nameof(identity));
        _logger = logger;
        _controller = controller;

        // DevInfoPage.protocolVersion (f1, uint16) equals the two bytes after "RideOn" in the reply on SyncTx:
        // KICKR 514 = 0x0202 -> "RideOn 02 02"; Victory 515 = 0x0203 -> "RideOn 02 03"; BLE Click v1 01 03, Play 01 04, Click v2 02 03 (bikecontrol constants.dart).
        _protocolVersion = controller switch
        {
            ControllerIdentity.ClickV1 => 0x0103,
            ControllerIdentity.Play => 0x0104,
            ControllerIdentity.ClickV2 => 0x0203,
            ControllerIdentity.BikeControlRide => 0x0203, // DevInfo f1 = 515; the RideOn reply is overridden below ("02 00")
            _ => 0x0202, // KickrBike, and Ride (Ride value unknown: KICKR fallback)
        };
        _capabilities = controller == ControllerIdentity.KickrBike
            ? new[] { (DeviceTypeCyclingTurboTrainer, 0x24u), (DeviceTypeUserInputDevice, 0u) }
            : new[] { (DeviceTypeUserInputDevice, 0u) };
        _rideOnReply = controller == ControllerIdentity.BikeControlRide
            ? new byte[] { 0x52, 0x69, 0x64, 0x65, 0x4F, 0x6E, 0x02, 0x00 } // captured BikeControl reply: "RideOn 02 00"
            : new byte[] { 0x52, 0x69, 0x64, 0x65, 0x4F, 0x6E, (byte)(_protocolVersion >> 8), (byte)(_protocolVersion & 0xFF) };

        if (controller != ControllerIdentity.KickrBike && controller != ControllerIdentity.BikeControlRide && logger is not null)
        {
            if (controller == ControllerIdentity.Ride)
                logger.LogWarning("Identity {Controller}: UNKNOWN protocolVersion / RideOn reply bytes of a real Zwift Ride, using the KICKR value 02 02", controller);
            logger.LogWarning("Identity {Controller}: UNKNOWN capability bitmask of USER_INPUT_DEVICE (using 0 as in the KICKR reply), manufacturerId/productId, firmware bytes, 2A29/2A24 strings, and the 0x400/0x303/0x210 replies; KICKR values are used for all of them", controller);
        }
    }

    /// <summary>The identity this handler presents.</summary>
    public ControllerIdentity Controller => _controller;

    /// <summary>True once a RideOn request has been answered.</summary>
    public bool RideOnCompleted { get; private set; }

    /// <summary>Raised when the RideOn handshake is answered the first time.</summary>
    public event EventHandler? RideOnHandled;

    // ---------- frame builders ----------

    /// <summary>
    /// Battery reply for get-request id 771 (0x303): 3c 08 83 06 (id 771) 12 08 { 08 00, 10 percent, 18 00, 20 00 }.
    /// The captured BikeControl reply is <c>3c08830612080800106418002000</c>, i.e. field 2 = 0x64 = 100 %. Percent is clamped to 0..100.
    /// </summary>
    public static byte[] BuildBatteryReply(int percent)
    {
        byte p = (byte)Math.Clamp(percent, 0, 100);
        return new byte[] { 0x3C, 0x08, 0x83, 0x06, 0x12, 0x08, 0x08, 0x00, 0x10, p, 0x18, 0x00, 0x20, 0x00 };
    }

    /// <summary>Builds the 0x23 keypad frame: opcode, then protobuf field 1 = varint(~pressedMask).</summary>
    public static byte[] BuildKeyPadFrame(ZwiftRideButton pressed)
    {
        uint inverted = ~(uint)pressed;
        var list = new List<byte>(8) { 0x23, 0x08 };
        WriteVarint(list, inverted);
        return list.ToArray();
    }

    /// <summary>Builds the periodic 0x03 ride-data frame: 08 power 10 cadence 18 0 20 0 28 0 30 0.</summary>
    public static byte[] BuildRideData(int powerWatts, int cadenceRpm)
    {
        var list = new List<byte>(16) { 0x03, 0x08 };
        WriteVarint(list, (uint)Math.Max(0, powerWatts));
        list.Add(0x10);
        WriteVarint(list, (uint)Math.Max(0, cadenceRpm));
        list.AddRange(new byte[] { 0x18, 0x00, 0x20, 0x00, 0x28, 0x00, 0x30, 0x00 });
        return list.ToArray();
    }

    /// <summary>
    /// Ride-data 0x03 frame with all captured fields: 08 power 10 cadence 18 f3 20 0 28 f5 30 0.
    /// From the capture f3 tracks speed (about cm/s: 358 at 12.7 km/h) and f5 is about 28.6 x f3 (wheel rpm x 100).
    /// Example from capture: (35, 72, 358, 10248) = 03 08 23 10 48 18 e6 02 20 00 28 88 50 30 00.
    /// </summary>
    public static byte[] BuildRideData(int powerWatts, int cadenceRpm, int field3, int field5)
    {
        var list = new List<byte>(20) { 0x03, 0x08 };
        WriteVarint(list, (uint)Math.Max(0, powerWatts));
        list.Add(0x10);
        WriteVarint(list, (uint)Math.Max(0, cadenceRpm));
        list.Add(0x18);
        WriteVarint(list, (uint)Math.Max(0, field3));
        list.AddRange(new byte[] { 0x20, 0x00, 0x28 });
        WriteVarint(list, (uint)Math.Max(0, field5));
        list.AddRange(new byte[] { 0x30, 0x00 });
        return list.ToArray();
    }

    /// <summary>
    /// Cycling Power Measurement (2A63), 16 bytes as in the capture: flags 0x003c (accumulated torque, torque source = crank,
    /// wheel revolution data, crank revolution data), then little endian: s16 power, u16 accumulated torque (1/32 Nm),
    /// u32 cumulative wheel revolutions, u16 last wheel event time (1/2048 s), u16 cumulative crank revolutions,
    /// u16 last crank event time (1/1024 s). Capture example: 3c00 2300 938a fd660000 4e78 4f73 4542.
    /// </summary>
    public static byte[] BuildPowerMeasurement(int powerWatts, ushort accumulatedTorque, uint wheelRevolutions, ushort wheelEventTime, ushort crankRevolutions, ushort crankEventTime)
    {
        var b = new byte[16];
        b[0] = 0x3C; b[1] = 0x00;
        int p = Math.Clamp(powerWatts, 0, short.MaxValue);
        b[2] = (byte)(p & 0xFF); b[3] = (byte)((p >> 8) & 0xFF);
        b[4] = (byte)(accumulatedTorque & 0xFF); b[5] = (byte)(accumulatedTorque >> 8);
        b[6] = (byte)(wheelRevolutions & 0xFF); b[7] = (byte)((wheelRevolutions >> 8) & 0xFF);
        b[8] = (byte)((wheelRevolutions >> 16) & 0xFF); b[9] = (byte)((wheelRevolutions >> 24) & 0xFF);
        b[10] = (byte)(wheelEventTime & 0xFF); b[11] = (byte)(wheelEventTime >> 8);
        b[12] = (byte)(crankRevolutions & 0xFF); b[13] = (byte)(crankRevolutions >> 8);
        b[14] = (byte)(crankEventTime & 0xFF); b[15] = (byte)(crankEventTime >> 8);
        return b;
    }

    /// <summary>
    /// Indoor Bike Data (2AD2), 8 bytes as in the capture: flags 0x0044 (bit0 = 0 so instantaneous speed is present,
    /// bit2 instantaneous cadence, bit6 instantaneous power), u16 speed (0.01 km/h), u16 cadence (0.5 rpm), s16 power.
    /// Capture example: 4400 7f06 8e00 2700 = 16.63 km/h, 71 rpm, 39 W.
    /// </summary>
    public static byte[] BuildIndoorBikeData(int speedCentiKmh, int cadenceRpm, int powerWatts)
    {
        int speed = Math.Clamp(speedCentiKmh, 0, ushort.MaxValue);
        int cad = Math.Clamp(cadenceRpm * 2, 0, ushort.MaxValue);
        int pw = Math.Clamp(powerWatts, short.MinValue, short.MaxValue);
        return new byte[]
        {
            0x44, 0x00,
            (byte)(speed & 0xFF), (byte)(speed >> 8),
            (byte)(cad & 0xFF), (byte)(cad >> 8),
            (byte)(pw & 0xFF), (byte)((pw >> 8) & 0xFF),
        };
    }

    /// <summary>
    /// 0x42 gear frame, front chainring part: 42 32 02 08 FF. Opcode 0x42, protobuf field 6 (tag 0x32, length-delimited)
    /// holding a sub-message {1: front chainring index}. The real bike sends it (with FF = 1) 12 ms before every rear cog frame.
    /// </summary>
    public static byte[] BuildChainringFrame(int frontChainring) => BuildGearSubMessage(0x08, frontChainring);

    /// <summary>
    /// 0x42 gear frame, rear cog part: 42 32 02 20 NN, field 6 holding a sub-message {4: rear cog index} (tag 0x20 = field 4 varint).
    /// In the capture NN moved by +/-1 per shift press and clamped at 1 (seen 1..5); Zwift's HUD shows it as "front-rear", e.g. "1-3".
    /// </summary>
    public static byte[] BuildRearCogFrame(int rearCog) => BuildGearSubMessage(0x20, rearCog);

    private static byte[] BuildGearSubMessage(byte tag, int value)
    {
        var inner = new List<byte> { tag };
        WriteVarint(inner, (uint)Math.Max(0, value));
        var list = new List<byte> { 0x42, 0x32, (byte)inner.Count };
        list.AddRange(inner);
        return list.ToArray();
    }

    /// <summary>
    /// Ride keypad frame with analog entries: 23 08 varint(~pressed) then one "1a LL 08 loc 10 zigzag(value)" per entry, as in the
    /// capture (neutral = 1a 04 08 00 10 00 1a 04 08 01 10 00). Location 0 = left paddle, 1 = right paddle (RideAnalogLocation in
    /// qdomyos-zwift / bikecontrol; left pulls negative). The capture's bike used the full +/-32767 range, Zwift Ride paddles use +/-100.
    /// </summary>
    public static byte[] BuildRideKeyPadFrame(ZwiftRideButton pressed, params RideAnalogEntry[] analog)
    {
        var list = new List<byte>(24);
        list.AddRange(BuildKeyPadFrame(pressed));
        foreach (var entry in analog)
        {
            var sub = new List<byte> { 0x08 };
            WriteVarint(sub, (uint)Math.Max(0, entry.Location));
            sub.Add(0x10);
            WriteVarint(sub, unchecked((uint)((entry.Value << 1) ^ (entry.Value >> 31)))); // sint32 zigzag
            list.Add(0x1A);
            list.Add((byte)sub.Count);
            list.AddRange(sub);
        }
        return list.ToArray();
    }

    /// <summary>
    /// Zwift Click v1 keypad frame: opcode 0x37 + ClickKeyPadStatus { 1: buttonPlus, 2: buttonMinus }.
    /// Verified against qdomyos-zwift (zwift_hub.proto, abstractZapDevice.h) and bikecontrol (emulated_peripherals.dart):
    /// enum PlayButtonStatus ON = 0 (pressed), OFF = 1 (released); proto2 optional fields are always written, so the frame is 5 bytes.
    /// Released: 37 08 01 10 01. Plus pressed: 37 08 00 10 01. Minus pressed: 37 08 01 10 00.
    /// </summary>
    public static byte[] BuildClickKeyPadFrame(bool plusPressed, bool minusPressed)
        => new byte[] { 0x37, 0x08, (byte)(plusPressed ? 0 : 1), 0x10, (byte)(minusPressed ? 0 : 1) };

    /// <summary>
    /// Zwift Play keypad frame: opcode 0x07 + PlayKeyPadStatus (fields 1..7 PlayButtonStatus where 0 = pressed and 1 = released;
    /// 8 = Analog_LR sint32, 9 = Analog_UD sint32). Field 1 (RightPad) is 0 for the right controller and 1 for the left one.
    /// A paddle press is Analog_LR = +/-100 (zigzag 200 = c8 01). Verified against qdomyos-zwift zwift_hub.proto and
    /// bikecontrol zwift_play.dart. Only the paddle is modelled here (all digital buttons released).
    /// </summary>
    public static byte[] BuildPlayKeyPadFrame(bool rightController, bool paddlePressed)
    {
        var list = new List<byte>(20)
        {
            0x07,
            0x08, (byte)(rightController ? 0 : 1),
            0x10, 0x01, 0x18, 0x01, 0x20, 0x01, 0x28, 0x01, 0x30, 0x01, 0x38, 0x01,
            0x40,
        };
        WriteVarint(list, paddlePressed ? 200u : 0u);
        list.AddRange(new byte[] { 0x48, 0x00 });
        return list.ToArray();
    }

    // ---------- request handling ----------

    /// <summary>Notifications to emit (in order) after a notification subscription is enabled.</summary>
    public IReadOnlyList<(BleUuid CharUuid, byte[] Notification)> OnSubscribed(BleUuid characteristic)
    {
        // The real bike sends two "ATX 08, STX xx" status strings on AsyncOut right after it is enabled; BikeControl sends nothing at that point.
        if (characteristic == DirconUuids.ZwiftAsync && _controller != ControllerIdentity.BikeControlRide)
            return new List<(BleUuid, byte[])>
            {
                (DirconUuids.ZwiftAsync, (byte[])AtxStatus0.Clone()),
                (DirconUuids.ZwiftAsync, (byte[])AtxStatus1.Clone()),
            };
        return Array.Empty<(BleUuid, byte[])>();
    }

    /// <summary>
    /// Handles a write to a characteristic. Returns notifications to send after the write ack.
    /// Unknown requests produce no notifications.
    /// </summary>
    public IReadOnlyList<(BleUuid CharUuid, byte[] Notification)> HandleWrite(BleUuid characteristic, byte[] value)
    {
        var result = new List<(BleUuid, byte[])>();
        if (value.Length == 0)
            return result;

        if (characteristic == DirconUuids.ZwiftSyncRx)
        {
            if (StartsWith(value, RideOnPrefix) && value.Length >= RideOnPrefix.Length + 2)
            {
                // Capture: "RIDE_ON(3)" status on AsyncOut, then RideOn 02 02 on SyncTx.
                // Choice: we answer EVERY RideOn (the real bike ignored the very first one, Zwift retried 9 s later).
                if (_controller == ControllerIdentity.BikeControlRide)
                {
                    // BikeControl capture: write ack, then RideOn 02 00 on 00000004, then the "RIDE_ON(0)" status on 00000002 (same millisecond).
                    result.Add((DirconUuids.ZwiftSyncTx, (byte[])_rideOnReply.Clone()));
                    result.Add((DirconUuids.ZwiftAsync, (byte[])BikeControlRideOnStatusText.Clone()));
                }
                else
                {
                    result.Add((DirconUuids.ZwiftAsync, (byte[])RideOnStatusText.Clone()));
                    result.Add((DirconUuids.ZwiftSyncTx, (byte[])_rideOnReply.Clone()));
                }
                bool first = !RideOnCompleted;
                RideOnCompleted = true;
                if (first)
                    RideOnHandled?.Invoke(this, EventArgs.Empty);
                return result;
            }

            HandleSyncRequest(value, result);
            return result;
        }

        if (characteristic == DirconUuids.WahooE037 && value[0] == 0x33)
        {
            result.Add((DirconUuids.WahooE037, (byte[])E037Reply.Clone()));
            return result;
        }

        _logger?.LogDebug("Unhandled write to {Uuid}: {Value}", characteristic, Convert.ToHexString(value));
        return result;
    }

    private void HandleSyncRequest(byte[] value, List<(BleUuid, byte[])> result)
    {
        byte opcode = value[0];
        var fields = ReadTopLevelVarints(value.AsSpan(1));
        if (_controller == ControllerIdentity.BikeControlRide)
        {
            // BikeControl answers only the four get-requests below; 0x41, 0x44, id 1280 and everything else stay unanswered.
            if (opcode == 0x00 && fields.TryGetValue(1, out uint bcId) && bcId == 771)
                result.Add((DirconUuids.ZwiftSyncTx, BuildBatteryReply(_batteryPercent?.Invoke() ?? 100)));
            else if (opcode == 0x00 && fields.TryGetValue(1, out bcId) && BikeControlGetResponses.TryGetValue(bcId, out var bcReply))
                result.Add((DirconUuids.ZwiftSyncTx, (byte[])bcReply.Clone()));
            else
                _logger?.LogDebug("BikeControl replica: no reply to {Value}", Convert.ToHexString(value));
            return;
        }
        switch (opcode)
        {
            case 0x41:
                // "41 08 05" -> "3e 08 41 10 02"
                result.Add((DirconUuids.ZwiftSyncTx, (byte[])Reply3e.Clone()));
                break;

            case 0x44:
                // SENSOR_RELAY_CONFIG ("44 20 01", "44 08 01" in the Victory capture) -> "3e 08 44 10 00", every identity.
                result.Add((DirconUuids.ZwiftSyncTx, (byte[])Reply3e44.Clone()));
                break;

            case 0x00:
                {
                    if (!fields.TryGetValue(1, out uint id))
                        break;
                    if (id == 0)
                        result.Add((DirconUuids.ZwiftSyncTx, BuildDeviceInfoReply()));
                    else if (id == 1280)
                    {
                        // Sensor relay page: only answered for the trainer-like KICKR identity (as the Victory does); a controller has no sensor relay.
                        if (_controller == ControllerIdentity.KickrBike)
                            result.Add((DirconUuids.ZwiftSyncTx, (byte[])SensorRelayPageReply.Clone()));
                        else
                            _logger?.LogDebug("No reply for get-request id 1280 (identity {Controller})", _controller);
                    }
                    else if (CannedGetResponses.TryGetValue(id, out var canned))
                        result.Add((DirconUuids.ZwiftSyncTx, (byte[])canned.Clone()));
                    else
                        _logger?.LogDebug("No reply for get-request id {Id} (capture: id 16 got no reply)", id);
                    break;
                }

            case 0x3F:
                {
                    _logger?.LogInformation("Zwift wrote 0x3F: {Hex} decoded: {Decoded}", Convert.ToHexString(value), DescribeProtobuf(value.AsSpan(1)));
                    // Choice: the capture shows "40 08 00 10 NN" only for the two setup writes (f1 == 1, f2 = 0 then 1),
                    // echoing f2. Later gear-state writes (f1 == 0, f2 >= 2) got no reply, so we send none.
                    if (fields.TryGetValue(1, out uint f1) && f1 == 1 && fields.TryGetValue(2, out uint f2))
                    {
                        var reply = new List<byte> { 0x40, 0x08, 0x00, 0x10 };
                        WriteVarint(reply, f2);
                        result.Add((DirconUuids.ZwiftSyncTx, reply.ToArray()));
                    }
                    break;
                }

            default:
                _logger?.LogDebug("Unknown Zwift sync opcode 0x{Opcode:X2}: {Value}", opcode, Convert.ToHexString(value));
                break;
        }
    }

    /// <summary>
    /// Device-information 0x3c reply to "00 08 00". Layout from capture:
    /// 3c 08 00 12 L1 0a L2 [08 82 04 12 04 12 00 15 01] 1a 0c name12 32 0f serial15 3a LL hw 42 04 08 01 10 24 42 04 08 02 10 00
    /// Name is truncated to 12 bytes; the serial field is "44" + serial padded with NULs to 15 bytes (as captured).
    /// Decoded with zp.proto: 3c = GetResponse{ 1: dataObjectId 0 (PAGE_DEV_INFO), 2: DevInfo{ 1: DevInfoPage } }. DevInfoPage: f1 protocolVersion
    /// (08 82 04 = 514 = 0x0202, NOT a device type), f2 systemFwVersion (4 bytes), f3 deviceName, f6 serialNumber, f7 systemHwRevision,
    /// f8 repeated deviceCapabilities{deviceType, capabilities} (1 = CYCLING_TURBO_TRAINER 0x24, 2 = USER_INPUT_DEVICE 0). The identity option
    /// changes f1 and the f8 list (controller identities list only USER_INPUT_DEVICE).
    /// </summary>
    public byte[] BuildDeviceInfoReply()
    {
        var name = Truncate(Encoding.ASCII.GetBytes(_identity.Name), 12);
        var serial = new byte[15];
        var serialSrc = Truncate(Encoding.ASCII.GetBytes("44" + _identity.SerialNumber), 15);
        serialSrc.CopyTo(serial, 0);
        var hw = Encoding.ASCII.GetBytes(_identity.HardwareRevision);

        var inner = new List<byte>();
        // f1 = protocolVersion (uint16; 514 = 08 82 04 for the KICKR), f2 = 4 firmware bytes (12 00 15 01 = 1.21.0.18).
        inner.Add(0x08); WriteVarint(inner, _protocolVersion);
        inner.AddRange(new byte[] { 0x12, 0x04, 0x12, 0x00, 0x15, 0x01 });
        inner.Add(0x1A); WriteVarint(inner, (uint)name.Length); inner.AddRange(name);
        inner.Add(0x32); WriteVarint(inner, (uint)serial.Length); inner.AddRange(serial);
        inner.Add(0x3A); WriteVarint(inner, (uint)hw.Length); inner.AddRange(hw);
        foreach (var (deviceType, capabilities) in _capabilities)
        {
            // f8 deviceCapabilities { 1: deviceType, 2: capabilities }
            var cap = new List<byte> { 0x08 };
            WriteVarint(cap, deviceType);
            cap.Add(0x10);
            WriteVarint(cap, capabilities);
            inner.Add(0x42); WriteVarint(inner, (uint)cap.Count); inner.AddRange(cap);
        }

        var mid = new List<byte> { 0x0A };
        WriteVarint(mid, (uint)inner.Count);
        mid.AddRange(inner);

        var outer = new List<byte> { 0x3C, 0x08, 0x00, 0x12 };
        WriteVarint(outer, (uint)mid.Count);
        outer.AddRange(mid);
        return outer.ToArray();
    }

    /// <summary>Decodes a 0x23 keypad frame for logs, e.g. "pressed=0x00000001 analog=[loc0=-100, loc1=0]" (raw ~mask 0xFFFFFFFF = nothing pressed).</summary>
    public static string DescribeKeyPadFrame(ReadOnlySpan<byte> frame)
    {
        if (frame.Length == 0 || frame[0] != 0x23)
            return "not a 0x23 frame";
        var data = frame[1..];
        int pos = 0;
        uint raw = 0xFFFFFFFF;
        var analog = new List<string>();
        while (pos < data.Length)
        {
            if (!TryReadVarint(data, ref pos, out ulong tag))
                return "malformed " + Convert.ToHexString(frame);
            int field = (int)(tag >> 3), wire = (int)(tag & 7);
            if (wire == 0)
            {
                if (!TryReadVarint(data, ref pos, out ulong v))
                    return "malformed " + Convert.ToHexString(frame);
                if (field == 1) raw = (uint)v;
            }
            else if (wire == 2)
            {
                if (!TryReadVarint(data, ref pos, out ulong len) || len > (ulong)(data.Length - pos))
                    return "malformed " + Convert.ToHexString(frame);
                var inner = data.Slice(pos, (int)len);
                pos += (int)len;
                int ip = 0; ulong loc = 0, zz = 0;
                while (ip < inner.Length && TryReadVarint(inner, ref ip, out ulong itag))
                {
                    if (!TryReadVarint(inner, ref ip, out ulong iv)) break;
                    if ((itag >> 3) == 1) loc = iv; else if ((itag >> 3) == 2) zz = iv;
                }
                long val = (long)(zz >> 1) ^ -(long)(zz & 1);
                analog.Add($"loc{loc}={val}");
            }
            else
                return "unsupported " + Convert.ToHexString(frame);
        }
        return $"pressed=0x{~raw:X8} analog=[{string.Join(", ", analog)}]";
    }

    /// <summary>Generic protobuf dump, e.g. "1:v=1 2:v=0 4:{8:v=5100 10:v=744}". Length-delimited fields are shown as a nested message when they parse fully, else as hex.</summary>
    public static string DescribeProtobuf(ReadOnlySpan<byte> data)
    {
        var sb = new StringBuilder();
        if (!TryDescribe(data, sb, 0))
        {
            sb.Clear();
            sb.Append("raw=").Append(Convert.ToHexString(data));
        }
        return sb.ToString();
    }

    private static bool TryDescribe(ReadOnlySpan<byte> data, StringBuilder sb, int depth)
    {
        int pos = 0;
        bool first = true;
        while (pos < data.Length)
        {
            if (!TryReadVarint(data, ref pos, out ulong tag))
                return false;
            int field = (int)(tag >> 3);
            int wire = (int)(tag & 7);
            if (field == 0)
                return false;
            if (!first) sb.Append(' ');
            first = false;
            if (wire == 0)
            {
                if (!TryReadVarint(data, ref pos, out ulong v))
                    return false;
                sb.Append(field).Append(":v=").Append(v);
            }
            else if (wire == 2)
            {
                if (!TryReadVarint(data, ref pos, out ulong len) || len > (ulong)(data.Length - pos))
                    return false;
                var inner = data.Slice(pos, (int)len);
                pos += (int)len;
                var nested = new StringBuilder();
                if (depth < 4 && inner.Length > 0 && TryDescribe(inner, nested, depth + 1))
                    sb.Append(field).Append(":{").Append(nested).Append('}');
                else
                    sb.Append(field).Append(":bytes=").Append(Convert.ToHexString(inner));
            }
            else if (wire == 5)
            {
                if (data.Length - pos < 4) return false;
                sb.Append(field).Append(":f32=").Append(Convert.ToHexString(data.Slice(pos, 4)));
                pos += 4;
            }
            else if (wire == 1)
            {
                if (data.Length - pos < 8) return false;
                sb.Append(field).Append(":f64=").Append(Convert.ToHexString(data.Slice(pos, 8)));
                pos += 8;
            }
            else
                return false;
        }
        return true;
    }

    // ---------- helpers ----------

    private static byte[] Truncate(byte[] src, int max) => src.Length <= max ? src : src.AsSpan(0, max).ToArray();

    private static bool StartsWith(byte[] value, byte[] prefix)
        => value.Length >= prefix.Length && value.AsSpan(0, prefix.Length).SequenceEqual(prefix);

    /// <summary>Appends a protobuf base-128 varint.</summary>
    public static void WriteVarint(List<byte> dest, uint value)
    {
        while (value >= 0x80)
        {
            dest.Add((byte)(value | 0x80));
            value >>= 7;
        }
        dest.Add((byte)value);
    }

    private static bool TryReadVarint(ReadOnlySpan<byte> data, ref int pos, out ulong value)
    {
        value = 0;
        int shift = 0;
        while (pos < data.Length && shift < 70)
        {
            byte b = data[pos++];
            value |= (ulong)(b & 0x7F) << shift;
            if ((b & 0x80) == 0)
                return true;
            shift += 7;
        }
        return false;
    }

    /// <summary>Reads top-level varint fields (field number -> last value); skips other wire types, stops on malformed data.</summary>
    private static Dictionary<int, uint> ReadTopLevelVarints(ReadOnlySpan<byte> data)
    {
        var fields = new Dictionary<int, uint>();
        int pos = 0;
        while (pos < data.Length)
        {
            if (!TryReadVarint(data, ref pos, out ulong tag))
                break;
            int field = (int)(tag >> 3);
            int wire = (int)(tag & 7);
            if (wire == 0)
            {
                if (!TryReadVarint(data, ref pos, out ulong v))
                    break;
                fields[field] = unchecked((uint)v);
            }
            else if (wire == 2)
            {
                if (!TryReadVarint(data, ref pos, out ulong len) || len > (ulong)(data.Length - pos))
                    break;
                pos += (int)len;
            }
            else if (wire == 5) pos += 4;
            else if (wire == 1) pos += 8;
            else break;
        }
        return fields;
    }
}

/// <summary>One analog axis entry of a ride keypad frame.</summary>
/// <param name="Location">0 = left paddle, 1 = right paddle, 2 = up, 3 = down.</param>
/// <param name="Value">Signed value (zigzag encoded on the wire).</param>
public readonly record struct RideAnalogEntry(int Location, int Value);
