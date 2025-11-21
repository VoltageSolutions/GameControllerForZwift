using Makaretu.Dns;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace GameControllerForZwift.Network.IntegrationTests
{
    /// <summary>
    /// Emulates a Zwift Play controller over ethernet
    /// </summary>
    public class ZwiftPlayEmulator : IDisposable
    {
        // Service and characteristic UUIDs from the codebase
        private static readonly Guid ServiceUuid = new Guid("00000001-19ca-4651-86e5-fa29dcdd09d1");
        private static readonly Guid WriteCharUuid = new Guid("00000003-19ca-4651-86e5-fa29dcdd09d1");
        private static readonly Guid NotifyCharUuid = new Guid("00000002-19ca-4651-86e5-fa29dcdd09d1");
        private static readonly Guid IndicateCharUuid = new Guid("00000004-19ca-4651-86e5-fa29dcdd09d1");

        // Expected request patterns from the codebase
        private static readonly byte[] ExpectedRequest1 = HexToBytes("52696465 4F6E02");
        private static readonly byte[] ExpectedRequest2 = HexToBytes("410805");
        private static readonly byte[] ExpectedRequest3 = HexToBytes("00088804");
        private static readonly byte[] ExpectedRequest4 = HexToBytes("042A0A10 C0BB0120");
        private static readonly byte[] ExpectedRequest5 = HexToBytes("0422");
        private static readonly byte[] ExpectedRequest6 = HexToBytes("042A0410");
        private static readonly byte[] ExpectedRequest7 = HexToBytes("042A0310");
        private static readonly byte[] ExpectedRequest8 = HexToBytes("0418");

        // Network components
        private TcpListener _listener;
        private TcpClient _client;
        private NetworkStream _stream;
        private int _port;
        private bool _isRunning;
        private CancellationTokenSource _cts;

        // mDNS components
        private MulticastService _mDNSService;
        private ServiceDiscovery _serviceDiscovery;
        private ServiceProfile _serviceProfile;
        private IPAddress _zwiftIPAddress;
        private int _zwiftPort;

        // Controller state
        public ControllerState State { get; private set; } = new ControllerState();

        // Power and cadence values
        public int Power { get; set; } = 150; // Default power in watts
        public int Cadence { get; set; } = 80; // Default cadence in rpm
        public float Speed { get; set; } = 25.0f; // Default speed in km/h

        /// <summary>
        /// Creates a new Zwift Play emulator
        /// </summary>
        /// <param name="port">TCP port to listen on</param>
        public ZwiftPlayEmulator(int port = 8888)
        {
            _port = port;
            _cts = new CancellationTokenSource();
            State.Reset();
        }

        /// <summary>
        /// Starts the emulator server
        /// </summary>
        public async Task StartAsync()
        {
            if (_isRunning)
                return;

            _isRunning = true;

            // Start mDNS advertisement
            StartMDNSAdvertisement();

            // Start TCP listener
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();

            Console.WriteLine($"Zwift Play emulator listening on port {_port}");

            try
            {
                while (_isRunning && !_cts.Token.IsCancellationRequested)
                {
                    // Wait for a client to connect
                    _client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    _stream = _client.GetStream();

                    Console.WriteLine("Client connected");

                    // Handle client communication
                    await HandleClientAsync(_cts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in server: {ex.Message}");
            }
            finally
            {
                _listener.Stop();
                _isRunning = false;
            }
        }

        /// <summary>
        /// Starts mDNS advertisement for the Zwift Play controller
        /// </summary>
        private void StartMDNSAdvertisement()
        {
            try
            {
                _mDNSService = new MulticastService();

                // Setup Event handlers for when the service is running
                _mDNSService.QueryReceived += (s, e) =>
                {
                    foreach (var question in e.Message.Questions)
                    {
                        // Listen for Zwift's discovery queries
                        if ((question.Name == "_wahoo-fitness-tnp._tcp.local") && (question.Type == DnsType.PTR))
                        {
                            _zwiftIPAddress = e.RemoteEndPoint.Address;
                            _zwiftPort = e.RemoteEndPoint.Port;

                            Console.WriteLine($"Zwift discovery query received from {_zwiftIPAddress}:{_zwiftPort}");

                            // Answer with our controller information
                            var response = new Message();

                            // PTR record
                            response.Answers.Add(new PTRRecord
                            {
                                Name = "_googlecast._tcp.local",
                                DomainName = "Zwift Play Controller_wahoo-fitness-tnp._tcp.local",
                                Class = DnsClass.IN,
                                Type = DnsType.PTR,
                                TTL = TimeSpan.FromSeconds(3600)
                            });

                            // SRV record
                            response.Answers.Add(new SRVRecord
                            {
                                Name = "Zwift Play Controller_wahoo-fitness-tnp._tcp.local.",
                                Target = "ZwiftPlayController.local.",
                                Port = (ushort)_port,
                                Priority = 0,
                                Weight = 0,
                                TTL = TimeSpan.FromSeconds(3600),
                            });

                            // A record
                            response.Answers.Add(new ARecord
                            {
                                Name = "ZwiftPlayController.local",
                                Address = GetLocalIPAddress(),
                                Class = DnsClass.IN,
                                TTL = TimeSpan.FromSeconds(3600),
                            });

                            // TXT record
                            response.Answers.Add(new TXTRecord
                            {
                                Name = "Zwift Play Controller._wahoo-fitness-tnp._tcp.local",
                                Type = DnsType.TXT,
                                Class = DnsClass.IN,
                                Strings = new List<string>
                                {
                                    "id=zwiftplay01",
                                    "cd=zwiftplay01",
                                    "rm=",
                                    "ve=05",
                                    "md=Zwift Play Controller",
                                    "ic=/setup/icon.png",
                                    "fn=Zwift Play Controller",
                                    "ca=199172",
                                    "st=0",
                                    "bs=FA8FCA301F25",
                                    "nf=1",
                                    "rs="
                                },
                                TTL = TimeSpan.FromSeconds(3600)
                            });

                            _mDNSService.SendAnswer(response);

                            Console.WriteLine("Sent mDNS response to Zwift");
                        }
                    }
                };

                _serviceDiscovery = new ServiceDiscovery(_mDNSService);
                _mDNSService.Start();

                // Create a service profile for our controller
                _serviceProfile = new ServiceProfile("Zwift Play Controller", "_wahoo-fitness-tnp._tcp.local", (ushort)_port);

                // Advertise our service
                _serviceDiscovery.Advertise(_serviceProfile);
                _serviceDiscovery.Announce(_serviceProfile);

                Console.WriteLine("Started mDNS advertisement for Zwift Play Controller");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting mDNS: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the local IP address
        /// </summary>
        private IPAddress GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            return IPAddress.Loopback;
        }

        /// <summary>
        /// Stops the emulator server
        /// </summary>
        public void Stop()
        {
            _cts.Cancel();
            _isRunning = false;
            _client?.Close();
            _listener?.Stop();

            // Stop mDNS
            _serviceDiscovery?.Unadvertise(_serviceProfile);
            _mDNSService?.Stop();
        }

        /// <summary>
        /// Updates the controller state and sends updates to Zwift
        /// </summary>
        /// <param name="newState">New controller state</param>
        public async Task UpdateStateAsync(ControllerState newState)
        {
            State = newState ?? throw new ArgumentNullException(nameof(newState));

            if (_client != null && _client.Connected)
            {
                // Send gear update if connected
                await SendGearUpdateAsync().ConfigureAwait(false);

                // Send power update
                await SendPowerUpdateAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Handles communication with a connected client
        /// </summary>
        private async Task HandleClientAsync(CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[1024];

            try
            {
                while (_client.Connected && !cancellationToken.IsCancellationRequested)
                {
                    // Check if there's data available to read
                    if (_client.Available > 0)
                    {
                        int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);

                        if (bytesRead > 0)
                        {
                            byte[] request = new byte[bytesRead];
                            Array.Copy(buffer, request, bytesRead);

                            await ProcessRequestAsync(request).ConfigureAwait(false);
                        }
                    }

                    // Send periodic updates (every 100ms)
                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                    await SendPeriodicUpdateAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Console.WriteLine($"Error handling client: {ex.Message}");
            }
            finally
            {
                _client.Close();
                Console.WriteLine("Client disconnected");
            }
        }

        /// <summary>
        /// Processes requests from Zwift
        /// </summary>
        private async Task ProcessRequestAsync(byte[] request)
        {
            Console.WriteLine($"Received request: {BitConverter.ToString(request).Replace("-", " ")}");

            if (StartsWith(request, ExpectedRequest1))
            {
                Console.WriteLine("Zwift Play Ask 1 - Initial handshake");

                // Send responses based on the codebase
                await SendResponseAsync(HexToBytes("2a08031211220f4154582030342c2053545820303400"), NotifyCharUuid).ConfigureAwait(false);
                await SendResponseAsync(HexToBytes("2a0803120d220b524944455f4f4e28322900"), NotifyCharUuid).ConfigureAwait(false);
                await SendResponseAsync(HexToBytes("526964654f6e0200"), IndicateCharUuid).ConfigureAwait(false);
            }
            else if (StartsWith(request, ExpectedRequest2))
            {
                Console.WriteLine("Zwift Play Ask 2 - Device information");

                await SendResponseAsync(HexToBytes("3c080012320a3008800412040500050"
                                                 + "11a0b4b49434b5220434f524500320f"
                                                 + "3430323431383030393834000000003a01314204080110140"), IndicateCharUuid).ConfigureAwait(false);
            }
            else if (StartsWith(request, ExpectedRequest3))
            {
                Console.WriteLine("Zwift Play Ask 3 - Configuration");

                await SendResponseAsync(HexToBytes("3c0888041206 0a0440c0bb01"), IndicateCharUuid).ConfigureAwait(false);
            }
            else if (StartsWith(request, ExpectedRequest4))
            {
                Console.WriteLine("Zwift Play Ask 4 - Parameters");

                await SendResponseAsync(HexToBytes("0308001000185920002800309bed01"), NotifyCharUuid).ConfigureAwait(false);
                await SendResponseAsync(HexToBytes("2a08031227222567"
                                                 + "61705f706172616d735f6368616e6765"
                                                 + "2832293a2037322c2037322c20302c20"
                                                 + "36303000"), NotifyCharUuid).ConfigureAwait(false);
            }
            else if (StartsWith(request, ExpectedRequest5))
            {
                Console.WriteLine("Zwift Play Ask 5 - Slope change");

                // Process slope change request (similar to decodeSInt in the codebase)
                if (request.Length >= 3)
                {
                    try
                    {
                        // Extract the slope value and process it
                        byte[] slopeBytes = new byte[request.Length - 1];
                        Array.Copy(request, 1, slopeBytes, 0, slopeBytes.Length);
                        int slope = ProtobufUtils.DecodeSInt32(slopeBytes);

                        Console.WriteLine($"Received slope change: {slope}");

                        // Acknowledge the slope change
                        await SendResponseAsync(HexToBytes("3c0888041206 0a0440c0bb01"), IndicateCharUuid).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing slope: {ex.Message}");
                    }
                }
            }
            else if (StartsWith(request, ExpectedRequest6) || StartsWith(request, ExpectedRequest7))
            {
                Console.WriteLine("Zwift Play Ask 6/7 - Gear change");

                if (request.Length >= 7)
                {
                    // Extract gear information from the request
                    byte[] gearBytes = new byte[3];
                    Array.Copy(request, 4, gearBytes, 0, Math.Min(3, request.Length - 4));

                    // Update gear based on the received bytes
                    UpdateGearFromBytes(gearBytes);

                    // Send acknowledgment
                    byte[] response = HexToBytes("3c0888041206 0a0440c0bb01");
                    // Copy the gear bytes into the response
                    Array.Copy(gearBytes, 0, response, 9, Math.Min(3, gearBytes.Length));
                    await SendResponseAsync(response, IndicateCharUuid).ConfigureAwait(false);

                    // Send additional response for Ask 7
                    if (StartsWith(request, ExpectedRequest7))
                    {
                        await SendResponseAsync(HexToBytes("03080010001827e7 2000 28 00 3093ed01"), NotifyCharUuid).ConfigureAwait(false);
                    }
                    else
                    {
                        await SendResponseAsync(HexToBytes("03080010001827e7 20002896143093ed01"), NotifyCharUuid).ConfigureAwait(false);
                    }
                }
            }
            else if (StartsWith(request, ExpectedRequest8))
            {
                Console.WriteLine("Zwift Play Ask 8 - Power request");

                if (request.Length >= 3)
                {
                    try
                    {
                        // Extract power value from the request
                        var (powerValue, _) = ProtobufUtils.DecodeVarint(request, 2);
                        Console.WriteLine($"Received power request: {powerValue}W");

                        // Send power update
                        byte[] response = HexToBytes("030882011022181020002898523086ed01");
                        response[2] = (byte)Power; // Set current power
                        await SendResponseAsync(response, NotifyCharUuid).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing power request: {ex.Message}");
                    }
                }
            }
            else
            {
                Console.WriteLine("Unknown request format");
            }
        }

        /// <summary>
        /// Sends a periodic update to Zwift with current controller state
        /// </summary>
        private async Task SendPeriodicUpdateAsync()
        {
            if (_client == null || !_client.Connected)
                return;

            try
            {
                // Send current power, cadence, and speed
                byte[] response = HexToBytes("0308001000185920002800309bed01");
                response[2] = (byte)Power;
                response[6] = (byte)Cadence;
                response[8] = (byte)(Speed * 100);
                response[9] = (byte)((int)(Speed * 100) >> 8);

                await SendResponseAsync(response, NotifyCharUuid).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending periodic update: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends a gear update based on the current controller state
        /// </summary>
        private async Task SendGearUpdateAsync()
        {
            if (_client == null || !_client.Connected)
                return;

            // Only send gear updates when buttons are pressed
            if (State.ArrowUp || State.ArrowDown)
            {
                try
                {
                    if (State.ArrowUp && State.CurrentGear < 24)
                    {
                        State.CurrentGear++;
                        Console.WriteLine($"Gear up to {State.CurrentGear}");
                    }
                    else if (State.ArrowDown && State.CurrentGear > 1)
                    {
                        State.CurrentGear--;
                        Console.WriteLine($"Gear down to {State.CurrentGear}");
                    }

                    // Reset button states
                    State.ArrowUp = false;
                    State.ArrowDown = false;

                    // Send gear update
                    byte[] gearBytes = State.GetGearBytes();
                    byte[] request = new byte[4 + gearBytes.Length];
                    request[0] = 0x04;
                    request[1] = 0x2A;
                    request[2] = 0x04;
                    request[3] = 0x10;
                    Array.Copy(gearBytes, 0, request, 4, gearBytes.Length);

                    await ProcessRequestAsync(request).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending gear update: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Sends a power update based on the analog inputs
        /// </summary>
        private async Task SendPowerUpdateAsync()
        {
            if (_client == null || !_client.Connected)
                return;

            try
            {
                // Map analog inputs to power (simple linear mapping)
                int newPower = 100 + (State.AnalogRight * 2);

                if (newPower != Power)
                {
                    Power = newPower;
                    Console.WriteLine($"Power updated to {Power}W");

                    // Create power request
                    byte[] powerBytes = ProtobufUtils.EncodeVarint((ulong)Power);
                    byte[] request = new byte[2 + powerBytes.Length];
                    request[0] = 0x04;
                    request[1] = 0x18;
                    Array.Copy(powerBytes, 0, request, 2, powerBytes.Length);

                    await ProcessRequestAsync(request).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending power update: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the current gear based on received bytes
        /// </summary>
        private void UpdateGearFromBytes(byte[] gearBytes)
        {
            // This is the reverse of GetGearBytes in ControllerState
            if (gearBytes.Length >= 2)
            {
                if (gearBytes[0] == 0xCC && gearBytes[1] == 0x3A) State.CurrentGear = 1;
                else if (gearBytes[0] == 0xFC && gearBytes[1] == 0x43) State.CurrentGear = 2;
                else if (gearBytes[0] == 0xAC && gearBytes[1] == 0x4D) State.CurrentGear = 3;
                else if (gearBytes[0] == 0xDC && gearBytes[1] == 0x56) State.CurrentGear = 4;
                else if (gearBytes[0] == 0x8C && gearBytes[1] == 0x60) State.CurrentGear = 5;
                else if (gearBytes[0] == 0xE8 && gearBytes[1] == 0x6B) State.CurrentGear = 6;
                else if (gearBytes[0] == 0xC4 && gearBytes[1] == 0x77) State.CurrentGear = 7;
                else if (gearBytes.Length >= 3)
                {
                    if (gearBytes[0] == 0xA0 && gearBytes[1] == 0x83 && gearBytes[2] == 0x01) State.CurrentGear = 8;
                    else if (gearBytes[0] == 0xA8 && gearBytes[1] == 0x91 && gearBytes[2] == 0x01) State.CurrentGear = 9;
                    else if (gearBytes[0] == 0xB0 && gearBytes[1] == 0x9F && gearBytes[2] == 0x01) State.CurrentGear = 10;
                    else if (gearBytes[0] == 0xB8 && gearBytes[1] == 0xAD && gearBytes[2] == 0x01) State.CurrentGear = 11;
                    else if (gearBytes[0] == 0xC0 && gearBytes[1] == 0xBB && gearBytes[2] == 0x01) State.CurrentGear = 12;
                    else if (gearBytes[0] == 0xF3 && gearBytes[1] == 0xCB && gearBytes[2] == 0x01) State.CurrentGear = 13;
                    else if (gearBytes[0] == 0xA8 && gearBytes[1] == 0xDC && gearBytes[2] == 0x01) State.CurrentGear = 14;
                    else if (gearBytes[0] == 0xDC && gearBytes[1] == 0xEC && gearBytes[2] == 0x01) State.CurrentGear = 15;
                    else if (gearBytes[0] == 0x90 && gearBytes[1] == 0xFD && gearBytes[2] == 0x01) State.CurrentGear = 16;
                    else if (gearBytes[0] == 0xD4 && gearBytes[1] == 0x90 && gearBytes[2] == 0x02) State.CurrentGear = 17;
                    else if (gearBytes[0] == 0x98 && gearBytes[1] == 0xA4 && gearBytes[2] == 0x02) State.CurrentGear = 18;
                    else if (gearBytes[0] == 0xDC && gearBytes[1] == 0xB7 && gearBytes[2] == 0x02) State.CurrentGear = 19;
                    else if (gearBytes[0] == 0x9F && gearBytes[1] == 0xCB && gearBytes[2] == 0x02) State.CurrentGear = 20;
                    else if (gearBytes[0] == 0xD8 && gearBytes[1] == 0xE2 && gearBytes[2] == 0x02) State.CurrentGear = 21;
                    else if (gearBytes[0] == 0x90 && gearBytes[1] == 0xFA && gearBytes[2] == 0x02) State.CurrentGear = 22;
                    else if (gearBytes[0] == 0xC8 && gearBytes[1] == 0x91 && gearBytes[2] == 0x03) State.CurrentGear = 23;
                    else if (gearBytes[0] == 0xF3 && gearBytes[1] == 0xAC && gearBytes[2] == 0x03) State.CurrentGear = 24;
                }
            }
        }

        /// <summary>
        /// Sends a response to Zwift
        /// </summary>
        private async Task SendResponseAsync(byte[] data, Guid characteristicUuid)
        {
            if (_client == null || !_client.Connected)
                return;

            try
            {
                // Create a packet with the characteristic UUID and data
                byte[] uuidBytes = characteristicUuid.ToByteArray();
                byte[] packet = new byte[16 + 4 + data.Length];

                // Copy UUID (16 bytes)
                Array.Copy(uuidBytes, 0, packet, 0, 16);

                // Add data length (4 bytes)
                BitConverter.GetBytes(data.Length).CopyTo(packet, 16);

                // Add data
                Array.Copy(data, 0, packet, 20, data.Length);

                // Send the packet
                await _stream.WriteAsync(packet, 0, packet.Length).ConfigureAwait(false);

                Console.WriteLine($"Sent response to {characteristicUuid}: {BitConverter.ToString(data).Replace("-", " ")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending response: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if a byte array starts with another byte array
        /// </summary>
        private static bool StartsWith(byte[] array, byte[] prefix)
        {
            if (array.Length < prefix.Length)
                return false;

            for (int i = 0; i < prefix.Length; i++)
            {
                if (array[i] != prefix[i])
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Converts a hex string to a byte array
        /// </summary>
        private static byte[] HexToBytes(string hex)
        {
            // Remove spaces and other non-hex characters
            hex = new string(hex.Where(c => char.IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')).ToArray());

            if (hex.Length % 2 != 0)
                throw new ArgumentException("Hex string must have an even number of characters");

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return bytes;
        }

        /// <summary>
        /// Processes button presses and updates the controller state
        /// </summary>
        public async Task ProcessButtonPressAsync(ControllerButton button, bool pressed)
        {
            switch (button)
            {
                case ControllerButton.A:
                    State.ButtonA = pressed;
                    break;
                case ControllerButton.B:
                    State.ButtonB = pressed;
                    break;
                case ControllerButton.Y:
                    State.ButtonY = pressed;
                    break;
                case ControllerButton.Z:
                    State.ButtonZ = pressed;
                    break;
                case ControllerButton.Up:
                    State.ArrowUp = pressed;
                    break;
                case ControllerButton.Down:
                    State.ArrowDown = pressed;
                    break;
                case ControllerButton.Left:
                    State.ArrowLeft = pressed;
                    break;
                case ControllerButton.Right:
                    State.ArrowRight = pressed;
                    break;
                case ControllerButton.LS:
                    State.ButtonLS = pressed;
                    break;
                case ControllerButton.RS:
                    State.ButtonRS = pressed;
                    break;
            }

            // Send updates immediately if connected
            if (_client != null && _client.Connected)
            {
                await SendButtonUpdateAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Updates analog inputs (ZL, ZR)
        /// </summary>
        public async Task UpdateAnalogInputsAsync(byte leftValue, byte rightValue)
        {
            State.AnalogLeft = leftValue;
            State.AnalogRight = rightValue;

            // Send updates immediately if connected
            if (_client != null && _client.Connected)
            {
                await SendAnalogUpdateAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Sends button state updates to Zwift
        /// </summary>
        private async Task SendButtonUpdateAsync()
        {
            // Create a button state packet
            byte buttonState = 0;
            if (State.ButtonA) buttonState |= 0x01;
            if (State.ButtonB) buttonState |= 0x02;
            if (State.ButtonY) buttonState |= 0x04;
            if (State.ButtonZ) buttonState |= 0x08;
            if (State.ArrowUp) buttonState |= 0x10;
            if (State.ArrowDown) buttonState |= 0x20;
            if (State.ArrowLeft) buttonState |= 0x40;
            if (State.ArrowRight) buttonState |= 0x80;

            byte extendedState = 0;
            if (State.ButtonLS) extendedState |= 0x01;
            if (State.ButtonRS) extendedState |= 0x02;

            // Send button state
            byte[] buttonPacket = new byte[] { 0x05, 0x01, buttonState, extendedState };
            await SendResponseAsync(buttonPacket, NotifyCharUuid).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends analog input updates to Zwift
        /// </summary>
        private async Task SendAnalogUpdateAsync()
        {
            // Create an analog input packet
            byte[] analogPacket = new byte[] { 0x06, 0x01, State.AnalogLeft, State.AnalogRight };
            await SendResponseAsync(analogPacket, NotifyCharUuid).ConfigureAwait(false);
        }

        /// <summary>
        /// Disposes resources
        /// </summary>
        public void Dispose()
        {
            Stop();
            _cts?.Dispose();
            _client?.Dispose();
            _mDNSService?.Dispose();
        }
    }
}