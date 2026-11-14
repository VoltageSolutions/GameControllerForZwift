using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;
using GameControllerForZwift.Network.DirectConnect;

namespace GameControllerForZwift.Network.ZwiftRide;

/// <summary>
/// Pure Zwift Ride controller protocol logic (no sockets): the handshake and request replies a Zwift Ride controller gives on the
/// Zwift characteristics, plus the keypad frame builders. The replies are byte-exact copies of the BikeControl capture
/// (docs/captures/BikeControl Loopback.pcapng); everything not listed there is deliberately left unanswered, as BikeControl does.
/// One instance per connection (tracks the handshake state).
/// </summary>
public sealed partial class ZwiftControlProtocol
{
    private static readonly byte[] RideOnPrefix = Encoding.ASCII.GetBytes("RideOn");

    /// <summary>Reply to "RideOn" on SyncTx: "RideOn" followed by 02 00.</summary>
    private static readonly byte[] RideOnReply = { 0x52, 0x69, 0x64, 0x65, 0x4F, 0x6E, 0x02, 0x00 };

    /// <summary>Status string sent on AsyncOut right after the RideOn reply: "RIDE_ON(0)" plus a NUL.</summary>
    private static readonly byte[] RideOnStatusText = Convert.FromHexString("2a0803120d220b524944455f4f4e28302900");

    // Replies to the four get-requests (opcode 0x00, field 1 = data object id) decoded from the capture.
    private static readonly Dictionary<uint, byte[]> GetResponses = new()
    {
        // DevInfoPage: protocol 515, fw 00 00 01 01, name "Zwift Ride", serial "0B-58D15ABB4363", hw "B.0", caps {2,1}, f9=1, f10=8, uid "58D15ABB4363".
        [0] = Convert.FromHexString("3c080012450a430883041204000001011a0a5a776966742052696465320f30422d3538443135414242343336333a03422e30420408021001480150085a0c353844313541424234333633"),
        [16] = Convert.FromHexString("3c081012020800"),
        [1024] = Convert.FromHexString("3c088008120608ffff031003"),
    };

    private const uint BatteryRequestId = 771;

    private readonly ILogger _logger;
    private readonly Func<int?>? _batteryPercent;

    /// <summary>Creates the protocol handler.</summary>
    /// <param name="logger">Optional logger.</param>
    /// <param name="batteryPercent">Optional provider of the battery percentage (0..100) reported in the id-771 reply; a null result keeps the captured 100.</param>
    public ZwiftControlProtocol(ILogger? logger = null, Func<int?>? batteryPercent = null)
    {
        _logger = logger ?? NullLogger.Instance;
        _batteryPercent = batteryPercent;
    }

    /// <summary>True once a RideOn request has been answered.</summary>
    public bool RideOnCompleted { get; private set; }

    /// <summary>Raised when the RideOn handshake is answered the first time.</summary>
    public event EventHandler? RideOnHandled;

    // ---------- frame builders ----------

    /// <summary>
    /// Battery reply for get-request id 771 (0x303): 3c 08 83 06 (id 771) 12 08 { 08 00, 10 percent, 18 00, 20 00 }.
    /// The captured reply is <c>3c08830612080800106418002000</c>, i.e. field 2 = 0x64 = 100 %. Percent is clamped to 0..100.
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

    /// <summary>
    /// Ride keypad frame with analog entries: 23 08 varint(~pressed) then one "1a LL 08 loc 10 zigzag(value)" per entry, as in the
    /// capture (neutral = 1a 04 08 00 10 00 1a 04 08 01 10 00). Location 0 = left paddle, 1 = right paddle (left pulls negative).
    /// Zwift Ride paddles use +/-100.
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

    // ---------- request handling ----------

    /// <summary>
    /// Handles a write to a characteristic. Returns notifications to send after the write ack.
    /// Unknown requests produce no notifications.
    /// </summary>
    public IReadOnlyList<(BleUuid CharUuid, byte[] Notification)> HandleWrite(BleUuid characteristic, byte[] value)
    {
        var result = new List<(BleUuid, byte[])>();
        if (value.Length == 0)
            return result;

        if (characteristic != ZwiftRideUuids.SyncRx)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                var hex = Convert.ToHexString(value);
                LogUnhandledWrite(characteristic, hex);
            }
            return result;
        }

        if (StartsWith(value, RideOnPrefix) && value.Length >= RideOnPrefix.Length + 2)
        {
            // Capture: write ack, then RideOn 02 00 on SyncTx, then the "RIDE_ON(0)" status on AsyncOut (same millisecond).
            // Every RideOn is answered, not only the first.
            result.Add((ZwiftRideUuids.SyncTx, (byte[])RideOnReply.Clone()));
            result.Add((ZwiftRideUuids.AsyncOut, (byte[])RideOnStatusText.Clone()));
            bool first = !RideOnCompleted;
            RideOnCompleted = true;
            if (first)
                RideOnHandled?.Invoke(this, EventArgs.Empty);
            return result;
        }

        HandleSyncRequest(value, result);
        return result;
    }

    private void HandleSyncRequest(byte[] value, List<(BleUuid, byte[])> result)
    {
        // Only the get-requests above are answered; 0x41, 0x44, id 1280 and everything else stay unanswered, as on the real replica.
        var fields = ReadTopLevelVarints(value.AsSpan(1));
        if (value[0] == 0x00 && fields.TryGetValue(1, out uint id))
        {
            if (id == BatteryRequestId)
            {
                result.Add((ZwiftRideUuids.SyncTx, BuildBatteryReply(_batteryPercent?.Invoke() ?? 100)));
                return;
            }
            if (GetResponses.TryGetValue(id, out var reply))
            {
                result.Add((ZwiftRideUuids.SyncTx, (byte[])reply.Clone()));
                return;
            }
        }
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            var hex = Convert.ToHexString(value);
            LogNoReply(hex);
        }
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

    // ---------- helpers ----------

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

    #region Logging

    [LoggerMessage(Level = LogLevel.Debug, Message = "Unhandled write to {Uuid}: {Value}")]
    private partial void LogUnhandledWrite(BleUuid uuid, string value);

    [LoggerMessage(Level = LogLevel.Debug, Message = "No reply to {Value}")]
    private partial void LogNoReply(string value);

    #endregion
}

/// <summary>One analog axis entry of a ride keypad frame.</summary>
/// <param name="Location">0 = left paddle, 1 = right paddle, 2 = up, 3 = down.</param>
/// <param name="Value">Signed value (zigzag encoded on the wire).</param>
public readonly record struct RideAnalogEntry(int Location, int Value);
