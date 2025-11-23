using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameControllerForZwift.Network
{
    // Minimal manual protobuf serializer for RideKeyPadStatus:
    // message RideKeyPadStatus {
    //   uint32 ButtonMap = 1;
    //   repeated RideAnalogKeyPress AnalogPaddles = 3; // not used by SendAction when empty
    // }
    public class RideKeyPadStatus
    {
        public uint ButtonMap { get; set; } = 0xFFFFFFFFu;
        // Analog paddles omitted in minimal implementation.
        // Add List<RideAnalogKeyPress> AnalogPaddles if you need to send them.

        public byte[] ToByteArray()
        {
            // Build protobuf wire-format bytes:
            // field 1 (varint) tag = (1 << 3) | 0 = 0x08
            using var ms = new MemoryStream();
            ms.WriteByte(0x08);
            WriteVarint(ms, ButtonMap);
            // If you later add analog paddles (field number 3), you'd write tag 0x1A then length-delimited submessage(s).
            return ms.ToArray();
        }

        // Write unsigned varint (protobuf base-128, little-endian groups with continuation bit).
        private static void WriteVarint(Stream s, uint value)
        {
            while (value >= 0x80)
            {
                s.WriteByte((byte)((value & 0x7F) | 0x80));
                value >>= 7;
            }
            s.WriteByte((byte)value);
        }
    }
}
