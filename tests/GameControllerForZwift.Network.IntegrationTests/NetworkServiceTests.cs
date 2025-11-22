using Castle.Components.DictionaryAdapter.Xml;
using Common.Logging;
using InTheHand.Bluetooth;
using Makaretu.Dns;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace GameControllerForZwift.Network.IntegrationTests
{


    public class NetworkServiceTests
    {
        private readonly ILogger<NetworkService> _loggerMock;
        private readonly NetworkService _networkService;


        private MulticastService? _mDNSServiceWahoo;
        private MulticastService? _mDNSServiceJetBlack;
        private MulticastService? _mDNSServiceKICKRBike;
        private IPAddress? _zwiftIPAddress;
        private int? _zwiftPort;


        private string deviceHostName = "KICKR BIKE SHIFT B84D";
        //private string deviceHostName = "Voltage Controller";

        const int Port = 36866;
        const byte ProtocolVersion = 0x01;

        // Mdns message ids / response codes (mirror MdnsConstants)
        const byte DC_RC_REQUEST_COMPLETED_SUCCESSFULLY = 0x00;
        const byte DC_MESSAGE_DISCOVER_SERVICES = 0x01;
        const byte DC_MESSAGE_DISCOVER_CHARACTERISTICS = 0x02;
        const byte DC_MESSAGE_READ_CHARACTERISTIC = 0x03;
        const byte DC_MESSAGE_WRITE_CHARACTERISTIC = 0x04;
        const byte DC_MESSAGE_ENABLE_CHARACTERISTIC_NOTIFICATIONS = 0x05;
        const byte DC_MESSAGE_CHARACTERISTIC_NOTIFICATION = 0x06;

        // Zwift custom UUIDs (lowercase, no dashes)
        static readonly string ZwiftServiceUuidNoDash = "0000fc8200001000800000805f9b34fb";
        static readonly string ZwiftAsyncCharacteristicUuidNoDash = "0000000219ca465186e5fa29dcdd09d1";
        static readonly string ZwiftSyncRxCharacteristicUuidNoDash = "0000000319ca465186e5fa29dcdd09d1";
        static readonly string ZwiftSyncTxCharacteristicUuidNoDash = "0000000419ca465186e5fa29dcdd09d1";

        // manufacturer / handshake bytes (RIDE_ON etc)
        static readonly byte[] RideOn = new byte[] { 0x52, 0x69, 0x64, 0x65, 0x4f, 0x6e }; // "RideOn"
        static readonly byte[] ResponseStartClickV2 = new byte[] { 0x02, 0x03 }; // as in Dart
        static readonly byte[] RideOnHandshake = RideOn.Concat(ResponseStartClickV2).ToArray();
        readonly ServiceDiscovery sd;
        TcpListener listener;
        TcpClient currentClient;
        NetworkStream currentStream;
        byte lastMessageId = 0;

        CancellationTokenSource internalCts = new CancellationTokenSource();


        public NetworkServiceTests()
        {
            _loggerMock = Substitute.For<ILogger<NetworkService>>();
            _networkService = new NetworkService(_loggerMock);
        }

        [Fact]
        public async Task AdvertiseTest()
        {
            Thread.Sleep(200000);

            //await RunWahooKICKRmDNS();

            //listener = new TcpListener(IPAddress.IPv6Any, Port);
            //listener.Server.DualMode = true; // accept IPv4 & IPv6
            //listener.Start();
            //CancellationToken cancellation = default;
            //var linked = CancellationTokenSource.CreateLinkedTokenSource(internalCts.Token, cancellation);

            //_ = Task.Run(() => AcceptLoopAsync(listener, linked.Token), linked.Token);

            //Thread.Sleep(20000);

            _mDNSServiceWahoo?.Stop();
            Assert.True(true);
        }

        async Task AcceptLoopAsync(TcpListener tcpListener, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var client = await tcpListener.AcceptTcpClientAsync(token).ConfigureAwait(false);
                    //var client = tcpListener.AcceptTcpClient();
                    System.Diagnostics.Debug.WriteLine($"Zwift is connected! {client.Client.RemoteEndPoint}");
                    // Replace current client
                    Interlocked.Exchange(ref currentClient, client)?.Close();
                    currentStream = client.GetStream();
                    _ = Task.Run(() => ClientLoopAsync(client, currentStream, token), token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
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
                    System.Diagnostics.Debug.WriteLine("Looking for messages...");
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

                        System.Diagnostics.Debug.WriteLine($"Parsed message: ID={msgId} seq={seqNum} len={length} body={ToHex(body)}");
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
                currentStream = null;
                currentClient = null;
            }
        }

        async Task HandleMessageAsync(TcpClient client, NetworkStream stream, byte msgVersion, byte msgId, byte seqNum, byte respCode, byte[] body)
        {
            switch (msgId)
            {
                case DC_MESSAGE_DISCOVER_SERVICES:
                    {
                        // Body expected to be 16-byte UUID or something; Dart simply returned the service UUID bytes
                        var bodyResponse = HexToBytes(ZwiftServiceUuidNoDash); // raw UUID bytes
                        var header = BuildHeader(msgVersion, DC_RC_REQUEST_COMPLETED_SUCCESSFULLY, seqNum, (ushort)bodyResponse.Length, DC_MESSAGE_DISCOVER_SERVICES);
                        await WriteAsync(stream, header.Concat(bodyResponse).ToArray());
                        break;
                    }
                case DC_MESSAGE_DISCOVER_CHARACTERISTICS:
                    {
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
                            await WriteAsync(stream, header.Concat(responseBody).ToArray());
                        }
                        break;
                    }
                case DC_MESSAGE_READ_CHARACTERISTIC:
                    {
                        // echo characteristic raw uuid as body (Dart behavior)
                        var rawUuid = body.Take(16).ToArray();
                        var header = BuildHeader(msgVersion, DC_RC_REQUEST_COMPLETED_SUCCESSFULLY, seqNum, (ushort)rawUuid.Length, DC_MESSAGE_READ_CHARACTERISTIC);
                        await WriteAsync(stream, header.Concat(rawUuid).ToArray());
                        break;
                    }
                case DC_MESSAGE_WRITE_CHARACTERISTIC:
                    {
                        // first 16 bytes = rawUUID, remainder = data
                        var rawUuid = body.Take(16).ToArray();
                        var rawData = body.Skip(16).ToArray();
                        var characteristicUuid = ToUuidString(rawUuid);

                        System.Diagnostics.Debug.WriteLine($"Write Characteristic {characteristicUuid}, data={ToHex(rawData)}");

                        // Respond with success & rawUuid as body (Dart)
                        var headerResp = BuildHeader(msgVersion, DC_RC_REQUEST_COMPLETED_SUCCESSFULLY, seqNum, (ushort)rawUuid.Length, DC_MESSAGE_WRITE_CHARACTERISTIC);
                        await WriteAsync(stream, headerResp.Concat(rawUuid).ToArray());

                        // If this is a write to SYNC RX, check handshake
                        var syncRx = ServiceUuidWithDashes(ZwiftSyncRxCharacteristicUuidNoDash);
                        if (characteristicUuid.Equals(syncRx, StringComparison.OrdinalIgnoreCase))
                        {
                            // compare with expected handshake
                            if (rawData.SequenceEqual(RideOnHandshake) || rawData.Take(RideOn.Length).SequenceEqual(RideOn))
                            {
                                System.Diagnostics.Debug.WriteLine("Got RIDE ON command!");
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

                                await WriteAsync(stream, header.Concat(responseBody).ToArray());
                            }
                        }
                        break;
                    }
                case DC_MESSAGE_ENABLE_CHARACTERISTIC_NOTIFICATIONS:
                    {
                        var rawUuid = body.Take(16).ToArray();
                        var enabled = body.Skip(16).FirstOrDefault();
                        System.Diagnostics.Debug.WriteLine($"Enable notifications for {ToUuidString(rawUuid)} enabled={enabled}");
                        var header = BuildHeader(msgVersion, DC_RC_REQUEST_COMPLETED_SUCCESSFULLY, seqNum, (ushort)rawUuid.Length, DC_MESSAGE_ENABLE_CHARACTERISTIC_NOTIFICATIONS);
                        await WriteAsync(stream, header.Concat(rawUuid).ToArray());
                        break;
                    }
                case DC_MESSAGE_CHARACTERISTIC_NOTIFICATION:
                    {
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
        byte[] BuildHeader(byte version, byte respCode, byte seqNum, ushort bodyLength, byte messageId)
        {
            var header = new byte[6];
            header[0] = version;
            header[1] = messageId;
            header[2] = seqNum;
            header[3] = respCode;
            BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(4, 2), bodyLength);
            return header;
        }

        int PropertyValue(IEnumerable<string> properties)
        {
            int res = 0;
            if (properties.Contains("read")) res |= 0x01;
            if (properties.Contains("write")) res |= 0x02;
            if (properties.Contains("indicate")) res |= 0x03;
            if (properties.Contains("notify")) res |= 0x04;
            return res;
        }

        // Expose a SendAction method that mirrors Dart sendAction
        // Replace InGameAction enum with your own mapping; below is a simplified example.
        public async Task<string> SendActionAsync(string inGameAction)
        {
            // Map action to button mask. Here we hardcode a single example.
            uint mask = inGameAction switch
            {
                "shiftUp" => 0x01000u, // SHFT_UP_R_BTN
                "shiftDown" => 0x00100u, // SHFT_UP_L_BTN
                "uturn" => 0x00008u, // DOWN_BTN
                _ => 0u
            };

            if (mask == 0u) return $"Action {inGameAction} not supported";

            // Construct RideKeyPadStatus protobuf message
            // Make sure you have generated the RideKeyPadStatus class from zwift.proto
            /*
            var status = new RideKeyPadStatus
            {
                ButtonMap = (~mask) & 0xFFFFFFFFu,
            };
            var bytes = status.ToByteArray();
            */

            // As a placeholder if you don't have proto available, create a protobuf-like byte[] (not recommended).
            // Replace the following with actual protobuf serialization above.
            byte[] protoBytes = BuildExampleRideKeyPadBytes(mask); // TODO: replace with status.ToByteArray()

            var payload = new byte[] { 0x23 } // CONTROLLER_NOTIFICATION opcode (35 decimal)
                .Concat(protoBytes)
                .ToArray();

            var notifyPacket = BuildButtonNotify(payload);

            if (currentStream == null)
                throw new InvalidOperationException("No client connected");

            await WriteAsync(currentStream, notifyPacket);
            return $"Sent action: {inGameAction}";
        }

        byte[] BuildButtonNotify(byte[] data)
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

        // Example proto bytes builder (temporary). Replace with real protobuf bytes from generated classes.
        byte[] BuildExampleRideKeyPadBytes(uint mask)
        {
            // This is NOT protobuf, only placeholder bytes; remove once you use real protobuf serialization.
            // Real usage:
            // var status = new RideKeyPadStatus { ButtonMap = (~mask) & 0xFFFFFFFFu };
            // return status.ToByteArray();
            return new byte[] { 0x00, 0x00, 0x00, 0x00 }; // placeholder
        }

        async Task WriteAsync(NetworkStream stream, byte[] data)
        {
            try
            {
                await stream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
                await stream.FlushAsync().ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine($"Sent response: {ToHex(data)}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WriteAsync exception: {ex}");
            }
        }

        static string ToUuidString(byte[] raw16)
        {
            return ServiceUuidWithDashes(BitConverter.ToString(raw16).Replace("-", "").ToLowerInvariant());
        }

        static string ServiceUuidWithDashes(string noDash)
        {
            noDash = noDash.Replace("-", "");
            if (noDash.Length != 32) return noDash;
            return $"{noDash.Substring(0, 8)}-{noDash.Substring(8, 4)}-{noDash.Substring(12, 4)}-{noDash.Substring(16, 4)}-{noDash.Substring(20)}";
        }

        static string ToHex(byte[] bytes) => BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();

        static byte[] HexToBytes(string hex)
        {
            var cleaned = hex.Replace("-", "").Replace("0x", "");
            if (cleaned.Length % 2 != 0) cleaned = "0" + cleaned;
            var len = cleaned.Length / 2;
            var res = new byte[len];
            for (int i = 0; i < len; i++)
                res[i] = Convert.ToByte(cleaned.Substring(i * 2, 2), 16);
            return res;
        }

        [Fact(Timeout = 20000)] // overall test timeout (20s) to keep CI safe
        public async Task MdnsEmulator_RunsForTenSeconds()
        {
            MdnsEmulator emulator = new MdnsEmulator();
            CancellationTokenSource cts = new();

            // Start the emulator (non-blocking)
            await emulator.StartAsync(cts.Token);

            // Wait ~10 seconds while the emulator advertises & listens
            await Task.Delay(TimeSpan.FromSeconds(15), cts.Token);

            // Optionally, you can assert on internal state if MdnsEmulator exposes it
            // e.g. Assert.True(emulator.IsAdvertising);

            // Stop / Dispose
            emulator.Dispose();
            cts.Cancel();
            cts.Dispose();

            Assert.True(true); // if we reach here, emulator ran without throwing
        }

        private TcpListener? _listener;

        public async Task RunWahooKICKRmDNS()
        {
            _mDNSServiceWahoo = new MulticastService();

            // Setup Event handlers for when the service is running.
            _mDNSServiceWahoo.QueryReceived += (s, e) =>
            {
                //System.Diagnostics.Debug.WriteLine($"Query Received from Endpoint: {e.RemoteEndPoint.Address}");
                //var names = e.Message.Questions
                //    .Select(q => q.Name + " " + q.Type);

                //System.Diagnostics.Debug.WriteLine("Queries received:");
                //foreach (var name in names)
                //    System.Diagnostics.Debug.WriteLine(name);


                foreach (var question in e.Message.Questions)
                {
                    if ((question.Name == "_wahoo-fitness-tnp._tcp.local") && (question.Type == DnsType.PTR))
                    {
                        System.Diagnostics.Debug.WriteLine($"Received query from {e.RemoteEndPoint.Address}");
                        _zwiftIPAddress = e.RemoteEndPoint.Address;
                        _zwiftPort = e.RemoteEndPoint.Port;

                        // Answer in this order
                        var response = new Message();
                        response.Answers.Add(new PTRRecord
                        {
                            Name = "_wahoo-fitness-tnp._tcp.local",
                            DomainName = deviceHostName + "._wahoo-fitness-tnp._tcp.local",
                            Class = DnsClass.IN,
                            Type = DnsType.PTR,
                            TTL = TimeSpan.FromSeconds(4500)
                        });

                        response.Answers.Add(new SRVRecord
                        {
                            Name = deviceHostName + "._wahoo-fitness-tnp._tcp.local",
                            Target = deviceHostName + ".local.",
                            Port = 36866,
                            Priority = 0,
                            Weight = 0,
                            TTL = TimeSpan.FromSeconds(4500),
                        });

                        response.Answers.Add(new ARecord
                        {
                            Name = deviceHostName + ".local",
                            Address = e.RemoteEndPoint.Address,
                            Class = DnsClass.IN,
                            TTL = TimeSpan.FromSeconds(3600),
                        });

                        response.Answers.Add(new TXTRecord
                        {
                            Name = deviceHostName + "._wahoo-fitness-tnp._tcp.local",
                            Type = DnsType.TXT,
                            Class = DnsClass.IN,
                            Strings = new List<string>
                            {
                                "ble-service-uuids=0xFC82,0x1818,0x1826,00000001-19CA-4651-86E5-FA29DCDD09D1,A026EE0D-0A7D-4AB3-97FA-F1500F9FEB8B",
                                "mac-address=68-67-25-6C-66-9C",
                                "serial-number=234700181"
                            },
                            TTL = TimeSpan.FromSeconds(3600)
                        });

                        _mDNSServiceWahoo.SendAnswer(response);
                        System.Diagnostics.Debug.WriteLine($"Responded to query from {e.RemoteEndPoint.Address}");
                    }
                }
            };
            _mDNSServiceWahoo.AnswerReceived += (s, e) =>
            {
                //System.Diagnostics.Debug.WriteLine($"Answer Received from Endpoint: {e.RemoteEndPoint.Address}");
                //var names = e.Message.Answers
                //    .Select(q => q.Name + " " + q.Type)
                //    .Distinct();

                //System.Diagnostics.Debug.WriteLine("Answers received:");
                //foreach (var name in names)
                //    System.Diagnostics.Debug.WriteLine(name);
            };


            var serviceDiscovery = new ServiceDiscovery(_mDNSServiceWahoo);
            _mDNSServiceWahoo.Start();

            var wahooServiceProfile = new ServiceProfile(deviceHostName, "_wahoo-fitness-tnp._tcp.local", 36866);

            // Make sure this service is unique / doesn't already exist on the netowrk
            if (!serviceDiscovery.Probe(wahooServiceProfile))
            {
                serviceDiscovery.Advertise(wahooServiceProfile);
                serviceDiscovery.Announce(wahooServiceProfile);
            }
        }

        public async Task RunJetBlackVoltagemDNS()
        {
            _mDNSServiceJetBlack = new MulticastService();

            // Setup Event handlers for when the service is running.
            _mDNSServiceJetBlack.QueryReceived += (s, e) =>
            {
                foreach (var question in e.Message.Questions)
                {
                    if ((question.Name == "_wahoo-fitness-tnp._tcp.local") && (question.Type == DnsType.PTR))
                    {
                        _zwiftIPAddress = e.RemoteEndPoint.Address;
                        _zwiftPort = e.RemoteEndPoint.Port;

                        // Answer in this order
                        var response = new Message();
                        response.Answers.Add(new PTRRecord
                        {
                            Name = "_wahoo-fitness-tnp._tcp.local",
                            DomainName = "Voltage GCFZ._wahoo-fitness-tnp._tcp.local",
                            Class = DnsClass.IN,
                            Type = DnsType.PTR,
                            TTL = TimeSpan.FromSeconds(3600)
                        });


                        response.Answers.Add(new SRVRecord
                        {
                            Name = "Voltage GCFZ._wahoo-fitness-tnp._tcp.",
                            Target = "Voltage GCFZHost.local.",
                            Port = 36866,
                            Priority = 0,
                            Weight = 0,
                            TTL = TimeSpan.FromSeconds(3600),
                        });

                        response.Answers.Add(new ARecord
                        {
                            Name = "Voltage GCFZH.local",
                            Address = e.RemoteEndPoint.Address,
                            Class = DnsClass.IN,
                            TTL = TimeSpan.FromSeconds(3600),
                        });

                        response.Answers.Add(new TXTRecord
                        {
                            Name = "Voltage GCFZ._wahoo-fitness-tnp._tcp.local",
                            Type = DnsType.TXT,
                            Class = DnsClass.IN,
                            Strings = new List<string>
                            {
                                //"ble-service-uuids=0x1800,0x1801,0x1826,0x1816,0xfe59,0x180d,180a",
                                "ble-service-uuids=0xFC82,0x1818,0x1826,00000001-19CA-4651-86E5-FA29DCDD09D1,A026EE0D-0A7D-4AB3-97FA-F1500F9FEB8BB",
                                //"mac-address=7c-2c-67-1d-4c-18",
                                "mac-address=68-67-25-1d-4c-18",
                                "serial-numer=123456789"
                            },
                            TTL = TimeSpan.FromSeconds(3600)
                        });
                        /*
                         * 0x1800 - GAP Service
                         * 0x1801 - GATT Service
                         * 0x1826 - Cycling Power Service
                         * 0x1816 - Cycling Speed and Cadence Service
                         * 0xfe59 - Wahoo Fitness Service
                         * 0x180d - Heart Rate Service
                         * 180a - Device Information Service
                         */

                        

                        _mDNSServiceJetBlack.SendAnswer(response);
                    }
                }
            };
            //_mDNSServiceJetBlack.AnswerReceived += (s, e) =>
            //{
            //};


            var serviceDiscovery = new ServiceDiscovery(_mDNSServiceJetBlack);
            _mDNSServiceJetBlack.Start();

            var voltageServiceProfile = new ServiceProfile("Voltage GCFZ", "_wahoo-fitness-tnp._tcp.local", 36866);

            // Make sure this service is unique / doesn't already exist on the netowrk
            if (!serviceDiscovery.Probe(voltageServiceProfile))
            {
                serviceDiscovery.Advertise(voltageServiceProfile);
                serviceDiscovery.Announce(voltageServiceProfile);
            }
        }

        private async Task WaitForRideOnMessage()
        {
            Console.WriteLine("Waiting for RideOn handshake...");

            // Set up a TCP listener to wait for a connection
            var listener = new TcpListener(IPAddress.Any, 36866);
            listener.Start();

            try
            {
                using var client = await listener.AcceptTcpClientAsync();
                System.Diagnostics.Debug.WriteLine("Connection accepted.");

                using var stream = client.GetStream();
                byte[] buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                string receivedMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                System.Diagnostics.Debug.WriteLine($"Received message: {receivedMessage}");

                if (receivedMessage == "RideOn")
                {
                    //rideOnReceived = true;

                    // Extract host IP address
                    //_zwiftIPAddress = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                    System.Diagnostics.Debug.WriteLine($"Handshake complete. Host IP: {_zwiftIPAddress}");

                    // Send acknowledgment
                    byte[] ack = Encoding.ASCII.GetBytes("RideOn");
                    await stream.WriteAsync(ack, 0, ack.Length);
                    System.Diagnostics.Debug.WriteLine("Acknowledgment sent.");
                }
            }
            finally
            {
                listener.Stop();
            }
        }

        private async Task StartTcpTransmission()
        {
            if (null == _zwiftIPAddress)
            {
                Console.WriteLine("No host IP address available. Exiting...");
                return;
            }

            Console.WriteLine($"Starting TCP transmission to {_zwiftIPAddress}...");

            var client = new TcpClient();

            try
            {
                await client.ConnectAsync(_zwiftIPAddress, _zwiftPort.Value);

                using var stream = client.GetStream();
                int counter = 0;

                while (true)
                {
                    // Generate and send sample data
                    string message = $"Data packet {counter++}";
                    byte[] data = Encoding.ASCII.GetBytes(message);
                    await stream.WriteAsync(data, 0, data.Length);
                    Console.WriteLine($"Sent: {message}");

                    await Task.Delay(1000); // Send data every second
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during TCP transmission: {ex.Message}");
            }
            finally
            {
                client.Close();
                Console.WriteLine("TCP transmission stopped.");
            }
        }
    }
}
