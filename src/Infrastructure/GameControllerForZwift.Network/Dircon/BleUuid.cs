using System.Buffers.Binary;
using System.Globalization;

namespace GameControllerForZwift.Network.Dircon;

/// <summary>
/// 128-bit Bluetooth UUID held in canonical (textual, big-endian) byte order.
/// Deliberately not System.Guid, whose byte order is mixed-endian.
/// </summary>
public readonly struct BleUuid : IEquatable<BleUuid>
{
    private const ulong BaseLow = 0x800000805F9B34FBUL;   // 80 00 00 80 5F 9B 34 FB

    private readonly ulong _high;
    private readonly ulong _low;

    private BleUuid(ulong high, ulong low)
    {
        _high = high;
        _low = low;
    }

    /// <summary>Builds from 16 bytes in canonical order (as written on the DIRCON wire).</summary>
    public static BleUuid FromBytes(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != 16)
            throw new ArgumentException("A UUID needs exactly 16 bytes.", nameof(bytes));
        return new BleUuid(
            BinaryPrimitives.ReadUInt64BigEndian(bytes.Slice(0, 8)),
            BinaryPrimitives.ReadUInt64BigEndian(bytes.Slice(8, 8)));
    }

    /// <summary>Builds from a 16-bit short form on the Bluetooth base UUID (0000xxxx-0000-1000-8000-00805F9B34FB).</summary>
    public static BleUuid FromShort(ushort shortUuid)
        => new(((ulong)shortUuid << 32) | 0x1000UL, BaseLow);

    /// <summary>Parses "xxxx", "0xXXXX" or the dashed / undashed 128-bit text form.</summary>
    public static BleUuid Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var s = text.Trim();
        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            s = s.Substring(2);
        if (s.Length == 4)
            return FromShort(ushort.Parse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
        s = s.Replace("-", string.Empty);
        if (s.Length != 32)
            throw new FormatException($"Invalid UUID '{text}'.");
        return FromBytes(Convert.FromHexString(s));
    }

    /// <summary>The 16 bytes in canonical (big-endian) order.</summary>
    public byte[] ToBytes()
    {
        var b = new byte[16];
        BinaryPrimitives.WriteUInt64BigEndian(b.AsSpan(0, 8), _high);
        BinaryPrimitives.WriteUInt64BigEndian(b.AsSpan(8, 8), _low);
        return b;
    }

    /// <summary>Writes the 16 canonical bytes into <paramref name="destination"/>.</summary>
    public void WriteTo(Span<byte> destination)
    {
        BinaryPrimitives.WriteUInt64BigEndian(destination.Slice(0, 8), _high);
        BinaryPrimitives.WriteUInt64BigEndian(destination.Slice(8, 8), _low);
    }

    public bool Equals(BleUuid other) => _high == other._high && _low == other._low;
    public override bool Equals(object? obj) => obj is BleUuid o && Equals(o);
    public override int GetHashCode() => HashCode.Combine(_high, _low);
    public static bool operator ==(BleUuid left, BleUuid right) => left.Equals(right);
    public static bool operator !=(BleUuid left, BleUuid right) => !left.Equals(right);

    /// <summary>Uppercase dashed form, e.g. 0000180A-0000-1000-8000-00805F9B34FB.</summary>
    public override string ToString()
    {
        var h = Convert.ToHexString(ToBytes());
        return $"{h.Substring(0, 8)}-{h.Substring(8, 4)}-{h.Substring(12, 4)}-{h.Substring(16, 4)}-{h.Substring(20, 12)}";
    }
}
