using GameControllerForZwift.Core;
using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

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
        static readonly string ZwiftServiceUuidNoDash = "0000fc8200001000800000805f9b34fb";
        static readonly string ZwiftAsyncCharacteristicUuidNoDash = "0000000219ca465186e5fa29dcdd09d1";
        static readonly string ZwiftSyncRxCharacteristicUuidNoDash = "0000000319ca465186e5fa29dcdd09d1";
        static readonly string ZwiftSyncTxCharacteristicUuidNoDash = "0000000419ca465186e5fa29dcdd09d1";

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

        public async Task AcceptTCPConnectionLoopAsync( CancellationToken token)
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

        //// Expose a SendAction method that mirrors Dart sendAction
        //// Replace InGameAction enum with your own mapping; below is a simplified example.
        //public async Task<string> SendActionAsync(string inGameAction)
        //{
        //    // Map action to button mask. Here we hardcode a single example.
        //    uint mask = inGameAction switch
        //    {
        //        "shiftUp" => 0x01000u, // SHFT_UP_R_BTN
        //        "shiftDown" => 0x00100u, // SHFT_UP_L_BTN
        //        "uturn" => 0x00008u, // DOWN_BTN
        //        _ => 0u
        //    };

        //    if (mask == 0u) return $"Action {inGameAction} not supported";

        //    // Construct RideKeyPadStatus protobuf message
        //    // Make sure you have generated the RideKeyPadStatus class from zwift.proto
        //    /*
        //    var status = new RideKeyPadStatus
        //    {
        //        ButtonMap = (~mask) & 0xFFFFFFFFu,
        //    };
        //    var bytes = status.ToByteArray();
        //    */

        //    // As a placeholder if you don't have proto available, create a protobuf-like byte[] (not recommended).
        //    // Replace the following with actual protobuf serialization above.
        //    byte[] protoBytes = BuildExampleRideKeyPadBytes(mask); // TODO: replace with status.ToByteArray()

        //    var payload = new byte[] { 0x23 } // CONTROLLER_NOTIFICATION opcode (35 decimal)
        //        .Concat(protoBytes)
        //        .ToArray();

        //    var notifyPacket = BuildButtonNotify(payload);

        //    if (_currentStream == null)
        //        throw new InvalidOperationException("No client connected");

        //    await WriteAsync(_currentStream, notifyPacket);
        //    return $"Sent action: {inGameAction}";
        //}

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

            // Build protobuf message
            var status = new RideKeyPadStatus
            {
                ButtonMap = (~buttonMask) & 0xFFFFFFFFu
            };
            //System.Diagnostics.Debug.WriteLine("protobuf status: " + status.ToString());
            // clear analog paddles if needed: status.AnalogPaddles.Clear();

            var protoBytes = status.ToByteArray(); // Google.Protobuf
            //System.Diagnostics.Debug.WriteLine("protobuf bytes: " + protoBytes.ToString());
            var payload = new byte[] { OpcodeControllerNotification }.Concat(protoBytes).ToArray();
            var notifyPacket = BuildButtonNotify(payload);

            System.Diagnostics.Debug.WriteLine("Notifying Zwift of button press");
            System.Diagnostics.Debug.WriteLine("Payload:" + BitConverter.ToString(payload).Replace("-", string.Empty));
            System.Diagnostics.Debug.WriteLine("Packet:" + BitConverter.ToString(notifyPacket).Replace("-", string.Empty));
            await WriteAsync(_currentStream, notifyPacket);

            // release packet: either send a zero/proprietary bytes or an "all ones" protobuf
            await Task.Delay(50);
            var releaseProto = new RideKeyPadStatus { ButtonMap = 0xFFFFFFFFu };
            var releasePayload = new byte[] { OpcodeControllerNotification }.Concat(releaseProto.ToByteArray()).ToArray();

            var releasePacket = BuildButtonNotify(releasePayload);
            System.Diagnostics.Debug.WriteLine("Sending Zwift all ones.");
            System.Diagnostics.Debug.WriteLine("Payload:" + BitConverter.ToString(releasePayload).Replace("-", string.Empty));
            System.Diagnostics.Debug.WriteLine("Packet:" + BitConverter.ToString(releasePacket).Replace("-", string.Empty));
            await WriteAsync(_currentStream, releasePacket);

            return "Sent";
        }

        



        #endregion

    }
}
