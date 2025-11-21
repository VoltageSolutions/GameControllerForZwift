using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameControllerForZwift.Network.IntegrationTests
{
    /// <summary>
    /// Utilities for Protocol Buffer encoding/decoding
    /// </summary>
    public static class ProtobufUtils
    {
        /// <summary>
        /// Result of decoding a varint
        /// </summary>
        public struct VarintResult
        {
            public ulong Value { get; }
            public int BytesRead { get; }

            public VarintResult(ulong value, int bytesRead)
            {
                Value = value;
                BytesRead = bytesRead;
            }

            public void Deconstruct(out ulong value, out int bytesRead)
            {
                value = Value;
                bytesRead = BytesRead;
            }
        }

        /// <summary>
        /// Decode a varint from a byte array
        /// </summary>
        public static VarintResult DecodeVarint(byte[] bytes, int startIndex)
        {
            ulong result = 0;
            int shift = 0;
            int bytesRead = 0;

            for (int i = startIndex; i < bytes.Length; i++)
            {
                byte b = bytes[i];
                result |= ((ulong)(b & 0x7F)) << shift;
                bytesRead++;

                if ((b & 0x80) == 0)
                    break;

                shift += 7;

                if (shift >= 64)
                    throw new OverflowException("Varint is too large");
            }

            return new VarintResult(result, bytesRead);
        }

        /// <summary>
        /// Encode a value as a varint
        /// </summary>
        public static byte[] EncodeVarint(ulong value)
        {
            var buffer = new List<byte>();

            while (value >= 0x80)
            {
                buffer.Add((byte)((value & 0x7F) | 0x80));
                value >>= 7;
            }

            buffer.Add((byte)value);

            return buffer.ToArray();
        }

        /// <summary>
        /// Decode a signed integer from a byte array using ZigZag encoding
        /// </summary>
        public static int DecodeSInt32(byte[] bytes)
        {
            var (value, _) = DecodeVarint(bytes, 0);

            // ZigZag decoding for signed integers
            // For a ulong, we need to handle the negation differently
            ulong zigzag = value >> 1;
            if ((value & 1) == 0)
            {
                // Even number (LSB = 0) means positive value
                return (int)zigzag;
            }
            else
            {
                // Odd number (LSB = 1) means negative value
                // Equivalent to XOR with -1 for the negative case
                return ~(int)zigzag;
            }
        }

        /// <summary>
        /// Encode a signed integer using ZigZag encoding
        /// </summary>
        public static byte[] EncodeSInt32(int value)
        {
            // ZigZag encoding: (n << 1) ^ (n >> 31)
            ulong zigzag = ((ulong)value << 1) ^ ((ulong)value >> 31);
            return EncodeVarint(zigzag);
        }
    }
}
