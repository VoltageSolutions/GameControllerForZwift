using GameControllerForZwift.Core;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace GameControllerForZwift.Network.Network
{
    public class TCPMessagingHelper
    {
        #region Fields

        private readonly ConcurrentDictionary<ZwiftFunction, CancellationTokenSource> _activeKeyPresses = new();
        private readonly TimeSpan _actionTimeout = TimeSpan.FromMilliseconds(50);
        private readonly ushort _port = 36866;

        // TCP Fields
        private readonly TcpListener _tcpListener;
        private TcpClient? _currentClient;
        private NetworkStream? _currentStream;


        // Zwift Communication
        private const string ZwiftServiceUuidNoDash = "0000fc8200001000800000805f9b34fb";
        private const string ZwiftAsyncCharacteristicUuidNoDash = "0000000219ca465186e5fa29dcdd09d1";
        private const string ZwiftSyncRxCharacteristicUuidNoDash = "0000000319ca465186e5fa29dcdd09d1";
        private const string ZwiftSyncTxCharacteristicUuidNoDash = "0000000419ca465186e5fa29dcdd09d1";

        static readonly byte[] RideOn = new byte[] { 0x52, 0x69, 0x64, 0x65, 0x4f, 0x6e }; // "RideOn"
        static readonly byte[] ResponseStartClickV2 = new byte[] { 0x02, 0x03 };
        static readonly byte[] RideOnHandshake = RideOn.Concat(ResponseStartClickV2).ToArray();

        /*
         Measurement, Notifiable: 00000002-19ca-4651-86e5-fa29dcdd09d1
Control Point (commands) Writable: 00000003-19ca-4651-86e5-fa29dcdd09d1
Command response, Indicable and Readable: 00000004-19ca-4651-86e5-fa29dcdd09d1
         
         
         */

        byte lastMessageId = 0;

        // variable name
        const byte ProtocolVersion = 0x01;

        const byte DC_RC_REQUEST_COMPLETED_SUCCESSFULLY = 0x00;
        const byte DC_MESSAGE_DISCOVER_SERVICES = 0x01;
        const byte DC_MESSAGE_DISCOVER_CHARACTERISTICS = 0x02;
        const byte DC_MESSAGE_READ_CHARACTERISTIC = 0x03;
        const byte DC_MESSAGE_WRITE_CHARACTERISTIC = 0x04;
        const byte DC_MESSAGE_ENABLE_CHARACTERISTIC_NOTIFICATIONS = 0x05;
        const byte DC_MESSAGE_CHARACTERISTIC_NOTIFICATION = 0x06;

        #endregion

        #region Constructor

        public TCPMessagingHelper()
        {
            _tcpListener = new TcpListener(IPAddress.IPv6Any, _port);
            _tcpListener.Server.DualMode = true; // accept IPv4 & IPv6
            _tcpListener.Start();
        }

        #endregion

        #region Methods

        public async Task AcceptTCPConnectionLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var client = await _tcpListener.AcceptTcpClientAsync(token).ConfigureAwait(false);
                    //_logger.LogDebug($"Client connected: {client.Client.RemoteEndPoint}");
                    System.Diagnostics.Debug.WriteLine($"Client connected: {client.Client.RemoteEndPoint}");
                    // Replace current client
                    Interlocked.Exchange(ref _currentClient, client)?.Close();
                    _currentStream = client.GetStream();
                    _ = Task.Run(() => ClientLoopAsync(client, _currentStream, token), token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error in TCP connection loop.");
                System.Diagnostics.Debug.WriteLine($"Accept loop error: {ex}");
            }
        }

        async Task ClientLoopAsync(TcpClient client, NetworkStream stream, CancellationToken token)
        {
            var buffer = new List<byte>();
            try
            {
                var readBuffer = new byte[4096];
                while (!token.IsCancellationRequested && client.Connected)
                {
                    var read = await stream.ReadAsync(readBuffer, 0, readBuffer.Length, token).ConfigureAwait(false);
                    if (read == 0) break;

                    buffer.AddRange(readBuffer.AsSpan(0, read).ToArray());
                    // Attempt to parse full messages while possible
                    while (buffer.Count >= 6)
                    {
                        var msgVersion = buffer[0];
                        var msgId = buffer[1];
                        var seqNum = buffer[2];
                        var respCode = buffer[3];
                        var length = BinaryPrimitives.ReadUInt16BigEndian(buffer.GetRange(4, 2).ToArray());

                        if (buffer.Count < 6 + length)
                            break; // wait for more bytes

                        var body = buffer.GetRange(6, length).ToArray();
                        // remove processed bytes
                        buffer.RemoveRange(0, 6 + length);

                        //System.Diagnostics.Debug.WriteLine($"Parsed message: ID={msgId} seq={seqNum} len={length} body={ToHex(body)}");
                        await HandleMessageAsync(client, stream, msgVersion, msgId, seqNum, respCode, body);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Client loop exception: {ex}");
            }
            finally
            {
                System.Diagnostics.Debug.WriteLine($"Client disconnected: {client.Client.RemoteEndPoint}");
                client.Close();
                // this method accepts parameters, but then uses local fields - not TDD
                _currentStream = null;
                _currentClient = null;
            }
        }

        public async Task HandleMessageAsync(TcpClient client, NetworkStream stream, byte msgVersion, byte msgId, byte seqNum, byte respCode, byte[] body)
        {
            switch (msgId)
            {
                case DC_MESSAGE_DISCOVER_SERVICES:
                    {
                        System.Diagnostics.Debug.WriteLine("Zwift message: Discover services.");
                        // Body expected to be 16-byte UUID or something; Dart simply returned the service UUID bytes
                        var bodyResponse = HexToBytes(ZwiftServiceUuidNoDash); // raw UUID bytes
                        var header = BuildHeader(msgVersion, DC_RC_REQUEST_COMPLETED_SUCCESSFULLY, seqNum, (ushort)bodyResponse.Length, DC_MESSAGE_DISCOVER_SERVICES);
                        System.Diagnostics.Debug.WriteLine("Responding with Zwift Service UUID.");
                        await WriteAsync(stream, header.Concat(bodyResponse).ToArray());
                        break;
                    }
                case DC_MESSAGE_DISCOVER_CHARACTERISTICS:
                    {
                        System.Diagnostics.Debug.WriteLine("Zwift message: Discover characteristics.");
                        // body contains the service uuid 16 bytes. We need to respond with characteristic UUIDs + property bytes
                        // Construct a response that mirrors Dart: [serviceRawUUID, syncRxUuid, prop(write), asyncUuid, prop(notify), syncTxUuid, prop(notify)]
                        var serviceRaw = body.Take(16).ToArray();
                        var serviceUuid = ToUuidString(serviceRaw);
                        if (serviceUuid.Equals(ServiceUuidWithDashes(ZwiftServiceUuidNoDash), StringComparison.OrdinalIgnoreCase))
                        {
                            var responseBody = new List<byte>();
                            responseBody.AddRange(serviceRaw);

                            // Sync RX
                            responseBody.AddRange(HexToBytes(ZwiftSyncRxCharacteristicUuidNoDash));
                            responseBody.Add((byte)PropertyValue(new[] { "write" }));

                            // Async notify
                            responseBody.AddRange(HexToBytes(ZwiftAsyncCharacteristicUuidNoDash));
                            responseBody.Add((byte)PropertyValue(new[] { "notify" }));

                            // Sync TX indicate/read/notify (echoing Dart)
                            responseBody.AddRange(HexToBytes(ZwiftSyncTxCharacteristicUuidNoDash));
                            responseBody.Add((byte)PropertyValue(new[] { "notify" }));

                            var header = BuildHeader(msgVersion, DC_RC_REQUEST_COMPLETED_SUCCESSFULLY, seqNum, (ushort)responseBody.Count, DC_MESSAGE_DISCOVER_CHARACTERISTICS);
                            System.Diagnostics.Debug.WriteLine("Responding with RX, Async, and TX characteristics.");
                            await WriteAsync(stream, header.Concat(responseBody).ToArray());
                        }
                        break;
                    }
                case DC_MESSAGE_READ_CHARACTERISTIC:
                    {
                        System.Diagnostics.Debug.WriteLine("Zwift message: Read a characteristic.");
                        // echo characteristic raw uuid as body (Dart behavior)
                        var rawUuid = body.Take(16).ToArray();
                        var header = BuildHeader(msgVersion, DC_RC_REQUEST_COMPLETED_SUCCESSFULLY, seqNum, (ushort)rawUuid.Length, DC_MESSAGE_READ_CHARACTERISTIC);
                        System.Diagnostics.Debug.WriteLine("Responding that the read request was successful.");
                        await WriteAsync(stream, header.Concat(rawUuid).ToArray());
                        break;
                    }
                case DC_MESSAGE_WRITE_CHARACTERISTIC:
                    {
                        System.Diagnostics.Debug.WriteLine("Zwift message: Write a characteristic.");
                        // first 16 bytes = rawUUID, remainder = data
                        var rawUuid = body.Take(16).ToArray();
                        var rawData = body.Skip(16).ToArray();
                        var characteristicUuid = ToUuidString(rawUuid);

                        System.Diagnostics.Debug.WriteLine($"Data={ToHex(rawData)}");

                        // Respond with success & rawUuid as body (Dart)
                        var headerResp = BuildHeader(msgVersion, DC_RC_REQUEST_COMPLETED_SUCCESSFULLY, seqNum, (ushort)rawUuid.Length, DC_MESSAGE_WRITE_CHARACTERISTIC);
                        System.Diagnostics.Debug.WriteLine("Responding that the request was successful.");
                        await WriteAsync(stream, headerResp.Concat(rawUuid).ToArray());

                        // If this is a write to SYNC RX, check handshake
                        var syncRx = ServiceUuidWithDashes(ZwiftSyncRxCharacteristicUuidNoDash);
                        if (characteristicUuid.Equals(syncRx, StringComparison.OrdinalIgnoreCase))
                        {
                            System.Diagnostics.Debug.WriteLine("This was a write to the Receive characteristic.");

                            // compare with expected handshake
                            if (rawData.SequenceEqual(RideOnHandshake) || rawData.Take(RideOn.Length).SequenceEqual(RideOn))
                            {
                                System.Diagnostics.Debug.WriteLine("Got the RideOn command!");
                                // send a CHARACTERISTIC_NOTIFICATION for SYNC TX with payload RideOn (matching Dart)
                                var seq = (byte)((lastMessageId + 1) & 0xFF);
                                lastMessageId = seq;

                                var responseBody = new List<byte>();
                                responseBody.AddRange(HexToBytes(ZwiftSyncTxCharacteristicUuidNoDash));
                                responseBody.AddRange(RideOnHandshake);

                                var header = new byte[6];
                                header[0] = ProtocolVersion;
                                header[1] = DC_MESSAGE_CHARACTERISTIC_NOTIFICATION;
                                header[2] = seq;
                                header[3] = DC_RC_REQUEST_COMPLETED_SUCCESSFULLY;
                                BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(4, 2), (ushort)responseBody.Count);

                                System.Diagnostics.Debug.WriteLine("Responding that we understood the RideOn!");
                                await WriteAsync(stream, header.Concat(responseBody).ToArray());
                            }
                        }
                        break;
                    }
                case DC_MESSAGE_ENABLE_CHARACTERISTIC_NOTIFICATIONS:
                    {
                        System.Diagnostics.Debug.WriteLine("Zwift message: Enable characteristic notifications.");
                        var rawUuid = body.Take(16).ToArray();
                        var enabled = body.Skip(16).FirstOrDefault();
                        var header = BuildHeader(msgVersion, DC_RC_REQUEST_COMPLETED_SUCCESSFULLY, seqNum, (ushort)rawUuid.Length, DC_MESSAGE_ENABLE_CHARACTERISTIC_NOTIFICATIONS);

                        System.Diagnostics.Debug.WriteLine("Responding that the enable-notification request was successful.");
                        await WriteAsync(stream, header.Concat(rawUuid).ToArray());

                        // If the async characteristic was enabled, record it so we only send notifications when accepted
                        var characteristicUuid = ToUuidString(rawUuid);
                        if (characteristicUuid.Equals(ServiceUuidWithDashes(ZwiftAsyncCharacteristicUuidNoDash), StringComparison.OrdinalIgnoreCase))
                        {
                            _asyncNotificationsEnabled = enabled != 0;
                            System.Diagnostics.Debug.WriteLine($"asyncNotificationsEnabled = {_asyncNotificationsEnabled}");
                        }
                        break;
                    }
                case DC_MESSAGE_CHARACTERISTIC_NOTIFICATION:
                    {
                        System.Diagnostics.Debug.WriteLine("Zwift message: Sent unexpected notification.");
                        System.Diagnostics.Debug.WriteLine("Client sent CHARACTERISTIC_NOTIFICATION (unexpected)");
                        break;
                    }
                default:
                    {
                        System.Diagnostics.Debug.WriteLine($"Unknown msgId: {msgId}");
                        break;
                    }
            }
        }

        // Build 6-byte header: [version, messageId, seqNum, respCode, lengthHi, lengthLo]
        public byte[] BuildHeader(byte version, byte respCode, byte seqNum, ushort bodyLength, byte messageId)
        {
            var header = new byte[6];
            header[0] = version;
            header[1] = messageId;
            header[2] = seqNum;
            header[3] = respCode;
            BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(4, 2), bodyLength);
            return header;
        }

        public int PropertyValue(IEnumerable<string> properties)
        {
            int res = 0;
            if (properties.Contains("read")) res |= 0x01;
            if (properties.Contains("write")) res |= 0x02;
            if (properties.Contains("indicate")) res |= 0x03;
            if (properties.Contains("notify")) res |= 0x04;
            return res;
        }

        public byte[] BuildButtonNotify(byte[] data)
        {
            var seq = (byte)((lastMessageId + 1) & 0xFF);
            lastMessageId = seq;

            var responseBody = new List<byte>();
            responseBody.AddRange(HexToBytes(ZwiftAsyncCharacteristicUuidNoDash));
            responseBody.AddRange(data);

            var header = new byte[6];
            header[0] = ProtocolVersion;
            header[1] = DC_MESSAGE_CHARACTERISTIC_NOTIFICATION;
            header[2] = seq;
            header[3] = DC_RC_REQUEST_COMPLETED_SUCCESSFULLY;
            BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(4, 2), (ushort)responseBody.Count);

            return header.Concat(responseBody).ToArray();
        }

        public async Task WriteAsync(NetworkStream stream, byte[] data)
        {
            try
            {
                await stream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
                await stream.FlushAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WriteAsync exception: {ex}");
                //_logger.LogError(ex, "Error writing to network stream.");
            }
        }

        public string ToHex(byte[] bytes) => BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();

        public byte[] HexToBytes(string hex)
        {
            var cleaned = hex.Replace("-", "").Replace("0x", "");
            if (cleaned.Length % 2 != 0) cleaned = "0" + cleaned;
            var len = cleaned.Length / 2;
            var res = new byte[len];
            for (int i = 0; i < len; i++)
                res[i] = Convert.ToByte(cleaned.Substring(i * 2, 2), 16);
            return res;
        }

        public string ToUuidString(byte[] raw16)
        {
            return ServiceUuidWithDashes(BitConverter.ToString(raw16).Replace("-", "").ToLowerInvariant());
        }

        public string ServiceUuidWithDashes(string noDash)
        {
            noDash = noDash.Replace("-", "");
            if (noDash.Length != 32) return noDash;
            return $"{noDash.Substring(0, 8)}-{noDash.Substring(8, 4)}-{noDash.Substring(12, 4)}-{noDash.Substring(16, 4)}-{noDash.Substring(20)}";
        }

        // Constants
        const byte OpcodeControllerNotification = 0x23; // 0x23 == 35

        // raw uuid strings (no dashes)


        //private int _lastMessageId = 0;
        private bool _asyncNotificationsEnabled = false; // set to true when you receive enable request


        // Send action (button press -> release)
        public async Task<string> SendActionAsync(uint buttonMask)
        {
            if (_currentStream == null || !_currentClient?.Connected == true)
                return "No client connected";

            if (!_asyncNotificationsEnabled)
                return "Notifications not enabled by client";

            // 1. DETERMINE BUTTON MASK BYTES
            byte[] buttonMaskBytes;
            string actionName;

            switch (buttonMask)
            {
                case 1: actionName = "Left Shift"; buttonMaskBytes = new byte[] { 0xFE, 0xFF, 0xFF, 0xFF }; break;
                case 4: actionName = "Right Shift"; buttonMaskBytes = new byte[] { 0xFB, 0xFF, 0xFF, 0xFF }; break;
                case 8: actionName = "Select"; buttonMaskBytes = new byte[] { 0xF7, 0xFF, 0xFF, 0xFF }; break;
                case 16: actionName = "Back"; buttonMaskBytes = new byte[] { 0xEF, 0xFF, 0xFF, 0xFF }; break;
                default:
                    System.Diagnostics.Debug.WriteLine($"Error: Unknown buttonMask {buttonMask}. Cannot send press.");
                    return "Unknown Mask";
            }

            // --- PRESS SEQUENCE ---
            _sequenceCounter = (byte)((_sequenceCounter + 1) % 256);
            var pressPayload = CreateKICKRPayload(buttonMaskBytes, _sequenceCounter);

            // finalPressPayload includes the OpcodeControllerNotification (e.g., 0x06)
            var finalPressPayload = new byte[] { OpcodeControllerNotification }.Concat(pressPayload).ToArray();
            var notifyPacket = BuildButtonNotify(finalPressPayload);

            System.Diagnostics.Debug.WriteLine($"Notifying Zwift of {actionName} (Mask: {buttonMask}, Seq: {_sequenceCounter})");
            System.Diagnostics.Debug.WriteLine("KICKR DATA (Press):" + BitConverter.ToString(pressPayload).Replace("-", string.Empty));
            await WriteAsync(_currentStream, notifyPacket);

            // --- RELEASE SEQUENCE ---
            await Task.Delay(50);

            _sequenceCounter = (byte)((_sequenceCounter + 1) % 256);
            var releasePayload = CreateKICKRPayload(KICKR_RELEASE_DATA_MASK, _sequenceCounter);

            var finalReleasePayload = new byte[] { OpcodeControllerNotification }.Concat(releasePayload).ToArray();
            var releasePacket = BuildButtonNotify(finalReleasePayload);

            System.Diagnostics.Debug.WriteLine($"Sending Zwift all ones (Release, Seq: {_sequenceCounter})");
            System.Diagnostics.Debug.WriteLine("KICKR DATA (Release):" + BitConverter.ToString(releasePayload).Replace("-", string.Empty));
            await WriteAsync(_currentStream, releasePacket);

            return $"Sent Hybrid KICKR Action for {actionName}";
        }

        private byte[] CreateKICKRPayload(byte[] statusBytes, byte sequence)
        {
            // Create a mutable copy of the trailer template
            var trailer = FixedButtonTrailerTemplate.ToArray();

            // Insert the 1-byte sequence counter into the target location
            // This overwrites the byte at index 4 (the fifth byte) of the 13-byte template (0x00)
            trailer[4] = sequence;

            // Build the final 19-byte KICKR DATA payload:
            // [23 08] + [4x Mask Bytes] + [13x Dynamic Trailer Bytes]

            var payload = new List<byte>
            {
                PropHeaderByte1, // 0x23 (Index 0)
                PropHeaderByte2  // 0x08 (Index 1)
            };

            // Add the 4 bytes of the Button Status Mask
            payload.AddRange(statusBytes); // (Indices 2-5)

            // Add the 13-byte trailer (now containing the 1-byte sequence counter)
            payload.AddRange(trailer); // (Indices 6-18)

            return payload.ToArray();
        }

        private static readonly byte[] PressedButtonDataPayload = new byte[]
        {
            // Protobuf Header (2 bytes)
            0x23, 0x08,
            // Button Status Field - PRESSED (4 bytes)
            0x00, 0x00, 0x00, 0x00,
            // Fixed Trailer (13 bytes)
            0x0F, 0x1A, 0x04, 0x08, 0x00, 0x10, 0x00, 0x1A, 0x04, 0x08, 0x01, 0x10, 0x00
        };

        // The full 19-byte payload that signals the button/shift event is idle (Released).
        // The status field is FF:FF:FF:FF, which is the default/idle state.
        private static readonly byte[] ReleasedButtonDataPayload = new byte[]
        {
            // Protobuf Header (2 bytes)
            0x23, 0x08,
            // Button Status Field - RELEASED (4 bytes)
            0xFF, 0xFF, 0xFF, 0xFF,
            // Fixed Trailer (13 bytes)
            0x0F, 0x1A, 0x04, 0x08, 0x00, 0x10, 0x00, 0x1A, 0x04, 0x08, 0x01, 0x10, 0x00
        };

        // The KICKR BIKE sends a fixed 13-byte trailer after the 4-byte button map.
        private static readonly byte[] FixedButtonTrailer = new byte[]
        {
            0x0F, 0x1A, 0x04, 0x08, 0x00, 0x10, 0x00, 0x1A, 0x04, 0x08, 0x01, 0x10, 0x00
        };

        // Assuming the proprietary header is 0x23, 0x08
        private const byte PropHeaderByte1 = 0x23;
        private const byte PropHeaderByte2 = 0x08;

        private byte[] CreateButtonPayload(uint buttonMask)
        {
            // The mask used by Zwift is inverted, so we follow the original logic.
            // However, since KICKR BIKE uses 0xFFFFFFFF for release, we only invert if not already 0xFFFFFFFF.
            uint buttonMap = buttonMask == 0xFFFFFFFFu ? 0xFFFFFFFFu : (~buttonMask) & 0xFFFFFFFFu;

            // The ButtonMap needs to be encoded into the Protobuf structure.
            // The protobuf definition for RideKeyPadStatus is assumed to be:
            // message RideKeyPadStatus { 
            //   fixed32 button_map = 1;
            //   repeated fixed32 analog_paddles = 2; // (Not used in this simplified example)
            // }

            // We MUST manually craft the protobuf bytes for the buttonMap to ensure a 4-byte fixed32 output.
            // Tag 1 (fixed32) is 0x0D.
            // 0x0D is the protobuf tag for 'field 1 (button_map)' with wire type 'fixed32'.

            // Convert the uint buttonMap to 4 little-endian bytes.
            byte[] buttonMapBytes = BitConverter.GetBytes(buttonMap);

            // This manual construction ensures we get a predictable 17-byte payload 
            // that contains the button data and the KICKR BIKE's proprietary trailer.
            // [0x0D] + [4 bytes: ButtonMap] + [12 bytes: Unknown]
            // Since we don't know the exact structure of the 17-byte protobuf payload
            // that produces the exact 13-byte trailer, we must rely on the KICKR BIKE's 
            // observation that the ButtonMap occupies bytes 3-6 of the 19-byte field.

            // Let's create the payload by combining the known pieces:
            // [Prop. Header: 2 bytes] + [Button Map: 4 bytes] + [Fixed Trailer: 13 bytes]

            var payload = new List<byte>
            {
                PropHeaderByte1,
                PropHeaderByte2
            };

            // Add the 4 bytes of the Button Map (in this position)
            payload.AddRange(buttonMapBytes);

            // Add the fixed 13-byte trailer
            payload.AddRange(FixedButtonTrailer);

            // This will produce a 19-byte array, where the 4 bytes in the middle 
            // are now the actual mask you intended to send.

            return payload.ToArray();
        }

        // Standard Button Masks (Inverted and Little-Endian)
        // --------------------------------------------------------------------------------
        // Mask 1 (Left/Down Shift) -> ~1 = 0xFFFFFFFE
        private static readonly byte[] KICKR_PRESS_LEFT_SHIFT = new byte[]
        {
    0x23, 0x08, 0xFE, 0xFF, 0xFF, 0xFF, 0x0F, 0x1A, 0x04, 0x08,
    0x00, 0x10, 0x00, 0x1A, 0x04, 0x08, 0x01, 0x10, 0x00
        };

        // Mask 4 (Right/Up Shift) -> ~4 = 0xFFFFFFFB
        private static readonly byte[] KICKR_PRESS_RIGHT_SHIFT = new byte[]
        {
    0x23, 0x08, 0xFB, 0xFF, 0xFF, 0xFF, 0x0F, 0x1A, 0x04, 0x08,
    0x00, 0x10, 0x00, 0x1A, 0x04, 0x08, 0x01, 0x10, 0x00
        };

        // Mask 8 (Select/A Button) -> ~8 = 0xFFFFFFF7
        private static readonly byte[] KICKR_PRESS_SELECT = new byte[]
        {
    0x23, 0x08, 0xF7, 0xFF, 0xFF, 0xFF, 0x0F, 0x1A, 0x04, 0x08,
    0x00, 0x10, 0x00, 0x1A, 0x04, 0x08, 0x01, 0x10, 0x00
        };

        // Mask 16 (Back/B Button) -> ~16 = 0xFFFFFFEF
        private static readonly byte[] KICKR_PRESS_BACK = new byte[]
        {
    0x23, 0x08, 0xEF, 0xFF, 0xFF, 0xFF, 0x0F, 0x1A, 0x04, 0x08,
    0x00, 0x10, 0x00, 0x1A, 0x04, 0x08, 0x01, 0x10, 0x00
        };
        // --------------------------------------------------------------------------------

        // KICKR BIKE Release/Neutral Event Payload (0xFFFFFFFF in status field) - Used for all releases
        // 23:08:FF:FF:FF:FF:0F:1A:04:08:00:10:00:1A:04:08:01:10:00
        private static readonly byte[] KICKR_RELEASE_DATA_19_BYTES = new byte[]
        {
    0x23, 0x08, 0xFF, 0xFF, 0xFF, 0xFF, 0x0F, 0x1A, 0x04, 0x08,
    0x00, 0x10, 0x00, 0x1A, 0x04, 0x08, 0x01, 0x10, 0x00
        };

        // KICKR BIKE Release/Neutral Event Payload (0xFFFFFFFF in status field)
        private static readonly byte[] KICKR_RELEASE_DATA_MASK = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };


        // ** Dynamic State Management **
        // Swapping the counter to a 4-byte unsigned integer (uint) for more robust sequencing.
        private byte _sequenceCounter = 0;
        // Private helper to manage the 13-byte trailer, making it mutable.
        // Original: 0F 1A 04 00 00 10 00 1A 04 08 01 10 00
        // The 13-byte trailer template, with the target byte (Index 4) set to 0xXX for the counter.
        // Original Trace: 0F 1A 04 08 [00] 10 00 1A 04 08 01 10 00
        private static readonly byte[] FixedButtonTrailerTemplate = new byte[]
        {
            0x0F, 0x1A, 0x04, 0x08, // 4 bytes of fixed header
            0x00,                   // *** TARGET: Index 4 (9 in 19-byte total) for Sequence Counter ***
            0x10, 0x00, 0x1A, 0x04, 0x08, 0x01, 0x10, 0x00 // 8 bytes of fixed trailer
        };

        // --- KICKR BIKE Static Mask Definitions (Hybrid Approach) ---
        //private const byte PropHeaderByte1 = 0x23;
        //private const byte PropHeaderByte2 = 0x08;

        #endregion
    }
}
