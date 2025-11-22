using GameControllerForZwift.Core;
using Makaretu.Dns;
using Microsoft.Extensions.Logging;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace GameControllerForZwift.Network
{
    public class NetworkService : IOutputService
    {
        #region Fields
        private readonly ILogger<NetworkService> _logger;
        private readonly ConcurrentDictionary<ZwiftFunction, CancellationTokenSource> _activeKeyPresses = new();
        private readonly TimeSpan _actionTimeout = TimeSpan.FromMilliseconds(50);
        //private readonly string _controllerName = "Voltage Controller";
        //private readonly string _controllerName = "KICKR BIKE SHIFT B84D";
        private readonly string _controllerName = "KICKR BIKE Voltage";

        // MDNS Fields
        private MulticastService? _mDNSControllerService;
        // make these const
        private readonly string _hostDomain = "_wahoo-fitness-tnp._tcp.local";
        private readonly ushort _port = 36866;
        private readonly string _bleServiceUUIDs = "ble-service-uuids=0xFC82,0x1818,0x1826,00000001-19CA-4651-86E5-FA29DCDD09D1,A026EE0D-0A7D-4AB3-97FA-F1500F9FEB8B";
        //private readonly string _bleServiceUUIDs = "ble-service-uuids=FC82";
        // todo - can we randomize these?
        private readonly string _macAddress = "mac-address=68-67-25-6C-66-9C";
        private readonly string _serialNumber = "serial-number=234700181";

        // TCP Fields
        private readonly TcpListener _listener;
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

        public NetworkService(ILogger<NetworkService> logger)
        {
            System.Diagnostics.Debug.WriteLine("NetworkService instantiated.");
            _logger = logger;

            var tokenSource = new CancellationTokenSource();
            _ = Task.Run(async () =>
            {
                try
                {
                    await AdvertiseControllerService();
                }
                catch (TaskCanceledException)
                {
                    // Timeout reset; key release will be handled later
                }
            }, tokenSource.Token);

            // Listen for both IPv4 and IPv6
            _listener = new TcpListener(IPAddress.IPv6Any, _port);
            _listener.Server.DualMode = true; // accept IPv4 & IPv6
            _listener.Start();

            _ = Task.Run(async () =>
            {
                try
                {
                    await AcceptTCPConnectionLoopAsync(_listener, tokenSource.Token);
                }
                catch (TaskCanceledException)
                {
                    // Timeout reset; key release will be handled later
                }
            }, tokenSource.Token);
        }

        #endregion

        // todo - dispose pattern to stop listener and mdns service
        //_mDNSServiceWahoo?.Stop();

        #region Methods
        public async Task<ActionResult> PerformActionAsync(ZwiftFunction zwiftFunction, ZwiftPlayerView playerView, ZwiftRiderAction riderAction)
        {
            //await _lock.WaitAsync();
            try
            {
                //VirtualKeyCode keyCode = GetKeyCode(zwiftFunction, playerView, riderAction);

                //if (keyCode == VirtualKeyCode.CANCEL)
                //{
                //    return new ActionResult { Success = false, ErrorMessage = "Unhandled Zwift function." };
                //}

                // Cancel any existing key press task for this function
                if (_activeKeyPresses.TryGetValue(zwiftFunction, out var existingTokenSource))
                {
                    existingTokenSource.Cancel();
                }
                else
                {
                    // Key isn't already held down - press it now
                    //_simulator.Keyboard.KeyDown(keyCode);
                }
                // Start the cancel-able task to release the key
                var tokenSource = new CancellationTokenSource();
                _activeKeyPresses[zwiftFunction] = tokenSource;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(_actionTimeout, tokenSource.Token);
                        //_simulator.Keyboard.KeyUp(keyCode);
                        //_activeKeyPresses.TryRemove(zwiftFunction, out _);
                    }
                    catch (TaskCanceledException)
                    {
                        // Timeout reset; key release will be handled later
                    }
                }, tokenSource.Token);

                return new ActionResult { Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing to network.");
                return new ActionResult { Success = false, ErrorMessage = ex.Message };
            }
            finally
            {
                //_lock.Release();
            }
        }

        // We will need somethign to take inputs and map them to the protobuf or format
        //public VirtualKeyCode GetKeyCode(ZwiftFunction zwiftFunction, ZwiftPlayerView playerView, ZwiftRiderAction riderAction)
        //{
        //    return zwiftFunction switch
        //    {
        //        ZwiftFunction.ShowMenu => VirtualKeyCode.UP,
        //        ZwiftFunction.NavigateLeft => VirtualKeyCode.LEFT,
        //        ZwiftFunction.NavigateRight => VirtualKeyCode.RIGHT,
        //        ZwiftFunction.Uturn => VirtualKeyCode.DOWN,
        //        ZwiftFunction.Powerup => VirtualKeyCode.SPACE,
        //        ZwiftFunction.Select => VirtualKeyCode.RETURN,
        //        ZwiftFunction.GoBack => VirtualKeyCode.ESCAPE,
        //        ZwiftFunction.ShowPairedDevices => VirtualKeyCode.VK_A,
        //        ZwiftFunction.ShowGarage => VirtualKeyCode.VK_T,
        //        ZwiftFunction.ToggleGraphs => VirtualKeyCode.VK_G,
        //        ZwiftFunction.SendGroupText => VirtualKeyCode.VK_M,
        //        ZwiftFunction.HideHUD => VirtualKeyCode.VK_H,
        //        ZwiftFunction.PromoCode => VirtualKeyCode.VK_P,
        //        ZwiftFunction.ShowTrainingMenu => VirtualKeyCode.VK_E,
        //        ZwiftFunction.SkipWorkoutBlock => VirtualKeyCode.TAB,
        //        ZwiftFunction.FTPBiasUp => VirtualKeyCode.PRIOR,
        //        ZwiftFunction.FTPBiasDown => VirtualKeyCode.NEXT,
        //        ZwiftFunction.AdjustCameraAngle => playerView switch
        //        {
        //            ZwiftPlayerView.Default => VirtualKeyCode.VK_1,
        //            ZwiftPlayerView.ThirdPerson => VirtualKeyCode.VK_2,
        //            ZwiftPlayerView.FPS => VirtualKeyCode.VK_3,
        //            ZwiftPlayerView.FrontLeftSide => VirtualKeyCode.VK_4,
        //            ZwiftPlayerView.RearRightSide => VirtualKeyCode.VK_5,
        //            ZwiftPlayerView.FacingRider => VirtualKeyCode.VK_6,
        //            ZwiftPlayerView.Spectator => VirtualKeyCode.VK_7,
        //            ZwiftPlayerView.Helicopter => VirtualKeyCode.VK_8,
        //            ZwiftPlayerView.BirdsEye => VirtualKeyCode.VK_9,
        //            ZwiftPlayerView.Drone => VirtualKeyCode.VK_0,
        //            _ => VirtualKeyCode.CANCEL
        //        },
        //        ZwiftFunction.RiderAction => riderAction switch
        //        {
        //            ZwiftRiderAction.Elbow => VirtualKeyCode.F1,
        //            ZwiftRiderAction.WaveHand => VirtualKeyCode.F2,
        //            ZwiftRiderAction.RideOn => VirtualKeyCode.F3,
        //            ZwiftRiderAction.HammerTime => VirtualKeyCode.F4,
        //            ZwiftRiderAction.Nice => VirtualKeyCode.F5,
        //            ZwiftRiderAction.BringIt => VirtualKeyCode.F6,
        //            ZwiftRiderAction.ImToast => VirtualKeyCode.F7,
        //            ZwiftRiderAction.BikeBell => VirtualKeyCode.F8,
        //            ZwiftRiderAction.ScreenShot => VirtualKeyCode.F10,
        //            _ => VirtualKeyCode.CANCEL
        //        },
        //        _ => VirtualKeyCode.CANCEL
        //    };
        //}


        // todo - cancellation token
        public async Task AdvertiseControllerService()
        {
            _mDNSControllerService = new MulticastService();

            // Setup Event handlers for when the service is running.
            _mDNSControllerService.QueryReceived += (s, e) =>
            {

                foreach (var question in e.Message.Questions)
                {
                    if ((question.Name == _hostDomain) && (question.Type == DnsType.PTR))
                    {
                        _logger.LogDebug($"Received query from {e.RemoteEndPoint.Address}");
                        System.Diagnostics.Debug.WriteLine($"Received query from {e.RemoteEndPoint.Address}");

                        // Answer in this order
                        var response = new Message();
                        response.Answers.Add(new PTRRecord
                        {
                            Name = _hostDomain,
                            // Connecting to Zwift seems to only work when this has the correct KICKR name.
                            DomainName = string.Concat(_controllerName, ".", _hostDomain),
                            Class = DnsClass.IN,
                            Type = DnsType.PTR,
                            //TTL = TimeSpan.FromSeconds(4500)
                            TTL = TimeSpan.FromSeconds(60)
                        });

                        response.Answers.Add(new SRVRecord
                        {
                            Name = string.Concat(_controllerName, ".", _hostDomain),
                            Target = string.Concat(_controllerName, ".local."),
                            Port = _port,
                            Priority = 0,
                            Weight = 0,
                            //TTL = TimeSpan.FromSeconds(4500)
                            TTL = TimeSpan.FromSeconds(60)
                        });

                        response.Answers.Add(new ARecord
                        {
                            Name = string.Concat(_controllerName, ".local."),
                            Address = e.RemoteEndPoint.Address,
                            Class = DnsClass.IN,
                            //TTL = TimeSpan.FromSeconds(3600)
                            TTL = TimeSpan.FromSeconds(60)
                        });

                        response.Answers.Add(new TXTRecord
                        {
                            Name = string.Concat(_controllerName, ".", _hostDomain),
                            Type = DnsType.TXT,
                            Class = DnsClass.IN,
                            Strings = new List<string>
                            {
                                _bleServiceUUIDs,
                                _macAddress,
                                _serialNumber
                            },
                            //TTL = TimeSpan.FromSeconds(3600)
                            TTL = TimeSpan.FromSeconds(60)
                        });

                        // Ensure the cache-flush flag is set on each answer record
                        foreach (var rr in response.Answers)
                        {
                            // Makaretu.Dns 2.0.1 does not expose CacheFlush/FlushCache properties
                            // on records. mDNS indicates cache-flush by setting the top bit
                            // (0x8000) in the CLASS field. As a fallback, set that bit on the
                            // record's Class property when possible.
                            if (rr is ResourceRecord baseRecord)
                            {
                                baseRecord.Class = (DnsClass)(((ushort)baseRecord.Class) | 0x8000);
                            }
                        }

                        _mDNSControllerService.SendAnswer(response);
                        _logger.LogDebug($"Responded to query from {e.RemoteEndPoint.Address}");
                        System.Diagnostics.Debug.WriteLine($"Responded to query from {e.RemoteEndPoint.Address}");
                    }
                }
            };

            var serviceDiscovery = new ServiceDiscovery(_mDNSControllerService);
            _mDNSControllerService.Start();

            var wahooServiceProfile = new ServiceProfile(_controllerName, _hostDomain, _port);

            // Make sure this service is unique / doesn't already exist on the netowrk
            if (!serviceDiscovery.Probe(wahooServiceProfile))
            {
                serviceDiscovery.Advertise(wahooServiceProfile);
                serviceDiscovery.Announce(wahooServiceProfile);
            }
        }

        async Task AcceptTCPConnectionLoopAsync(TcpListener tcpListener, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var client = await tcpListener.AcceptTcpClientAsync(token).ConfigureAwait(false);
                    _logger.LogDebug($"Client connected: {client.Client.RemoteEndPoint}");
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
                _logger.LogError(ex, "Error in TCP connection loop.");
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

        async Task HandleMessageAsync(TcpClient client, NetworkStream stream, byte msgVersion, byte msgId, byte seqNum, byte respCode, byte[] body)
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
                        //System.Diagnostics.Debug.WriteLine($"Enable notifications for {ToUuidString(rawUuid)} enabled={enabled}");
                        var header = BuildHeader(msgVersion, DC_RC_REQUEST_COMPLETED_SUCCESSFULLY, seqNum, (ushort)rawUuid.Length, DC_MESSAGE_ENABLE_CHARACTERISTIC_NOTIFICATIONS);

                        System.Diagnostics.Debug.WriteLine("Responding that the enable-notification request was successful.");
                        await WriteAsync(stream, header.Concat(rawUuid).ToArray());
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

            if (_currentStream == null)
                throw new InvalidOperationException("No client connected");

            await WriteAsync(_currentStream, notifyPacket);
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
                //System.Diagnostics.Debug.WriteLine($"Sent response: {ToHex(data)}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WriteAsync exception: {ex}");
            }
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
        #endregion
    }
}
