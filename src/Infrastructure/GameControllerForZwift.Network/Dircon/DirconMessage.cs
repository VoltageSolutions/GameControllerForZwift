using System.Buffers.Binary;

namespace GameControllerForZwift.Network.Dircon;

/// <summary>DIRCON message identifiers (byte 1 of the header).</summary>
public static class DirconMessageId
{
    /// <summary>Discover services.</summary>
    public const byte DiscoverServices = 0x01;
    /// <summary>Discover characteristics of one service.</summary>
    public const byte DiscoverCharacteristics = 0x02;
    /// <summary>Read a characteristic.</summary>
    public const byte ReadCharacteristic = 0x03;
    /// <summary>Write a characteristic.</summary>
    public const byte WriteCharacteristic = 0x04;
    /// <summary>Enable / disable notifications for a characteristic.</summary>
    public const byte EnableNotifications = 0x05;
    /// <summary>Unsolicited characteristic notification (server to client, seq 0).</summary>
    public const byte Notification = 0x06;
}

/// <summary>DIRCON response codes (byte 3 of the header).</summary>
public static class DirconResponseCode
{
    public const byte Success = 0;
    public const byte UnknownMessageType = 1;
    public const byte UnexpectedError = 2;
    public const byte ServiceNotFound = 3;
    public const byte CharacteristicNotFound = 4;
    public const byte OperationNotSupported = 5;
    public const byte WriteFailedInvalidSize = 6;
    public const byte UnknownProtocolVersion = 7;
}

/// <summary>
/// One DIRCON frame: 6-byte header (version, msgId, seq, respCode, uint16 big-endian length) plus payload.
/// </summary>
public sealed record DirconMessage(byte Version, byte MessageId, byte Seq, byte ResponseCode, byte[] Payload)
{
    /// <summary>Header size in bytes.</summary>
    public const int HeaderSize = 6;

    /// <summary>Protocol version used on the wire.</summary>
    public const byte CurrentVersion = 1;

    /// <summary>Creates a version-1 message.</summary>
    public static DirconMessage Create(byte messageId, byte seq, byte responseCode, byte[]? payload = null)
        => new(CurrentVersion, messageId, seq, responseCode, payload ?? Array.Empty<byte>());

    /// <summary>Serialises the message to wire bytes.</summary>
    public byte[] Encode()
    {
        if (Payload.Length > ushort.MaxValue)
            throw new InvalidOperationException("DIRCON payload too large.");
        var buf = new byte[HeaderSize + Payload.Length];
        buf[0] = Version;
        buf[1] = MessageId;
        buf[2] = Seq;
        buf[3] = ResponseCode;
        BinaryPrimitives.WriteUInt16BigEndian(buf.AsSpan(4, 2), (ushort)Payload.Length);
        Payload.CopyTo(buf.AsSpan(HeaderSize));
        return buf;
    }

    /// <summary>
    /// Tries to parse one message from the start of <paramref name="buffer"/>.
    /// Returns false (consumed = 0) if the buffer does not yet hold a complete frame.
    /// </summary>
    public static bool TryParse(ReadOnlySpan<byte> buffer, out DirconMessage? message, out int consumed)
    {
        message = null;
        consumed = 0;
        if (buffer.Length < HeaderSize)
            return false;
        int length = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(4, 2));
        int total = HeaderSize + length;
        if (buffer.Length < total)
            return false;
        message = new DirconMessage(buffer[0], buffer[1], buffer[2], buffer[3], buffer.Slice(HeaderSize, length).ToArray());
        consumed = total;
        return true;
    }
}
