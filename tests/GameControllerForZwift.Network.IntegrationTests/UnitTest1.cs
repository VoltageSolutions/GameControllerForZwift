using Castle.Components.DictionaryAdapter.Xml;
using Common.Logging;
using InTheHand.Bluetooth;
using Makaretu.Dns;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace GameControllerForZwift.Network.IntegrationTests
{
    public class ZwiftRideTcpServer
    {
        private TcpListener _listener;
        private int _port;

        public ZwiftRideTcpServer(int port)
        {
            _port = port;
            _listener = new TcpListener(IPAddress.Any, _port);
        }

        public async Task StartAsync()
        {
            _listener.Start();
            Console.WriteLine($"Zwift Ride TCP server listening on port {_port}");

            while (true)
            {
                var client = await _listener.AcceptTcpClientAsync();
                _ = HandleClientAsync(client);
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            var endpoint = client.Client.RemoteEndPoint;
            Console.WriteLine($"Zwift connected from {endpoint}");

            using (var stream = client.GetStream())
            {
                var buffer = new byte[1024];
                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    var received = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"Received: {received}");

                    // Example: If Zwift sends a "get characteristics" request, respond with mock data
                    if (received.Contains("characteristics", StringComparison.OrdinalIgnoreCase))
                    {
                        var response = @"{
                    ""service_uuid"": ""0000FC82-0000-1000-8000-00805F9B34FB"",
                    ""characteristics"": [
                        { ""uuid"": ""00000002-19CA-4651-86E5-FA29DCDD09D1"", ""type"": ""notify"" },
                        { ""uuid"": ""00000003-19CA-4651-86E5-FA29DCDD09D1"", ""type"": ""write"" },
                        { ""uuid"": ""00000004-19CA-4651-86E5-FA29DCDD09D1"", ""type"": ""indicate"" }
                    ]
                }";
                        var responseBytes = Encoding.UTF8.GetBytes(response);
                        await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                    }
                    else
                    {
                        // Echo or send a default response
                        var response = "Zwift Ride device here!";
                        var responseBytes = Encoding.UTF8.GetBytes(response);
                        await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                    }
                }
            }
        }
    }

    public class UnitTest1
    {
        private MulticastService? _mDNSServiceWahoo;
        private MulticastService? _mDNSServiceJetBlack;
        private MulticastService? _mDNSServiceKICKRBike;
        private IPAddress? _zwiftIPAddress;
        private int? _zwiftPort;

        [Fact]
        public async Task Test1()
        {
            //RunWahooKICKRmDNS();
            //await ZwiftPlayEmulatorTask();
            //await RunJetBlackVoltagemDNS();
            await RunWahooKICKRmDNS();

            //var server = new ZwiftRideTcpServer(36866);
            //await server.StartAsync();

            //await RunKickrBIKEmDNS();

            Thread.Sleep(10000);
            System.Diagnostics.Debug.WriteLine($"Zwift IP Address: {_zwiftIPAddress}");
            System.Diagnostics.Debug.WriteLine($"Zwift Port: {_zwiftPort}");

            //await WaitForRideOnMessage();

            /*
            var client = new TcpClient();
            await client.ConnectAsync(ipAddress, port); // Use the appropriate port
            using var stream = client.GetStream();
            var message = Encoding.UTF8.GetBytes("Hello from Wahoo KICKR");
            await stream.WriteAsync(message, 0, message.Length);
            */

            _mDNSServiceWahoo?.Stop();
            //_mDNSServiceJetBlack?.Stop();
            //_mDNSServiceKICKRBike?.Stop();
            Assert.True(true);
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

        public async Task ZwiftPlayEmulatorTask()
        {
            // Create and start the emulator
            using var emulator = new ZwiftPlayEmulator();

            // Start the emulator in a background task
            var serverTask = emulator.StartAsync();

            Console.WriteLine("Zwift Play emulator started. Press keys to control:");
            Console.WriteLine("A, B, Y, Z - Action buttons");
            Console.WriteLine("Arrow keys - Direction buttons");
            Console.WriteLine("L, R - Side buttons (LS, RS)");
            Console.WriteLine("1-9 - Set analog left (ZL)");
            Console.WriteLine("Shift+1-9 - Set analog right (ZR)");
            Console.WriteLine("Q - Quit");

            // Process keyboard input to control the emulator
            bool running = true;
            while (running)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);

                    switch (key.Key)
                    {
                        case ConsoleKey.A:
                            await emulator.ProcessButtonPressAsync(ControllerButton.A, true);
                            await Task.Delay(100);
                            await emulator.ProcessButtonPressAsync(ControllerButton.A, false);
                            Console.WriteLine("Button A pressed");
                            break;
                        case ConsoleKey.B:
                            await emulator.ProcessButtonPressAsync(ControllerButton.B, true);
                            await Task.Delay(100);
                            await emulator.ProcessButtonPressAsync(ControllerButton.B, false);
                            Console.WriteLine("Button B pressed");
                            break;
                        case ConsoleKey.Y:
                            await emulator.ProcessButtonPressAsync(ControllerButton.Y, true);
                            await Task.Delay(100);
                            await emulator.ProcessButtonPressAsync(ControllerButton.Y, false);
                            Console.WriteLine("Button Y pressed");
                            break;
                        case ConsoleKey.Z:
                            await emulator.ProcessButtonPressAsync(ControllerButton.Z, true);
                            await Task.Delay(100);
                            await emulator.ProcessButtonPressAsync(ControllerButton.Z, false);
                            Console.WriteLine("Button Z pressed");
                            break;
                        case ConsoleKey.UpArrow:
                            await emulator.ProcessButtonPressAsync(ControllerButton.Up, true);
                            await Task.Delay(100);
                            await emulator.ProcessButtonPressAsync(ControllerButton.Up, false);
                            Console.WriteLine("Up arrow pressed");
                            break;
                        case ConsoleKey.DownArrow:
                            await emulator.ProcessButtonPressAsync(ControllerButton.Down, true);
                            await Task.Delay(100);
                            await emulator.ProcessButtonPressAsync(ControllerButton.Down, false);
                            Console.WriteLine("Down arrow pressed");
                            break;
                        case ConsoleKey.LeftArrow:
                            await emulator.ProcessButtonPressAsync(ControllerButton.Left, true);
                            await Task.Delay(100);
                            await emulator.ProcessButtonPressAsync(ControllerButton.Left, false);
                            Console.WriteLine("Left arrow pressed");
                            break;
                        case ConsoleKey.RightArrow:
                            await emulator.ProcessButtonPressAsync(ControllerButton.Right, true);
                            await Task.Delay(100);
                            await emulator.ProcessButtonPressAsync(ControllerButton.Right, false);
                            Console.WriteLine("Right arrow pressed");
                            break;
                        case ConsoleKey.L:
                            await emulator.ProcessButtonPressAsync(ControllerButton.LS, true);
                            await Task.Delay(100);
                            await emulator.ProcessButtonPressAsync(ControllerButton.LS, false);
                            Console.WriteLine("LS button pressed");
                            break;
                        case ConsoleKey.R:
                            await emulator.ProcessButtonPressAsync(ControllerButton.RS, true);
                            await Task.Delay(100);
                            await emulator.ProcessButtonPressAsync(ControllerButton.RS, false);
                            Console.WriteLine("RS button pressed");
                            break;
                        case ConsoleKey.D1:
                        case ConsoleKey.D2:
                        case ConsoleKey.D3:
                        case ConsoleKey.D4:
                        case ConsoleKey.D5:
                        case ConsoleKey.D6:
                        case ConsoleKey.D7:
                        case ConsoleKey.D8:
                        case ConsoleKey.D9:
                            // Set analog values (1-9 maps to values 0-255)
                            byte value = (byte)(((int)key.Key - (int)ConsoleKey.D1) * 28);
                            if (key.Modifiers.HasFlag(ConsoleModifiers.Shift))
                            {
                                // Shift+number sets right analog (ZR)
                                await emulator.UpdateAnalogInputsAsync(emulator.State.AnalogLeft, value);
                                Console.WriteLine($"Set analog right (ZR) to {value}");
                            }
                            else
                            {
                                // Number sets left analog (ZL)
                                await emulator.UpdateAnalogInputsAsync(value, emulator.State.AnalogRight);
                                Console.WriteLine($"Set analog left (ZL) to {value}");
                            }
                            break;
                        case ConsoleKey.Add:
                        case ConsoleKey.OemPlus:
                            // Increase power
                            emulator.Power = Math.Min(emulator.Power + 10, 1000);
                            Console.WriteLine($"Power increased to {emulator.Power}W");
                            break;
                        case ConsoleKey.Subtract:
                        case ConsoleKey.OemMinus:
                            // Decrease power
                            emulator.Power = Math.Max(emulator.Power - 10, 50);
                            Console.WriteLine($"Power decreased to {emulator.Power}W");
                            break;
                        case ConsoleKey.PageUp:
                            // Increase cadence
                            emulator.Cadence = Math.Min(emulator.Cadence + 5, 150);
                            Console.WriteLine($"Cadence increased to {emulator.Cadence}rpm");
                            break;
                        case ConsoleKey.PageDown:
                            // Decrease cadence
                            emulator.Cadence = Math.Max(emulator.Cadence - 5, 30);
                            Console.WriteLine($"Cadence decreased to {emulator.Cadence}rpm");
                            break;
                        case ConsoleKey.Home:
                            // Increase speed
                            emulator.Speed = Math.Min(emulator.Speed + 1.0f, 60.0f);
                            Console.WriteLine($"Speed increased to {emulator.Speed:F1}km/h");
                            break;
                        case ConsoleKey.End:
                            // Decrease speed
                            emulator.Speed = Math.Max(emulator.Speed - 1.0f, 5.0f);
                            Console.WriteLine($"Speed decreased to {emulator.Speed:F1}km/h");
                            break;
                        case ConsoleKey.Q:
                            running = false;
                            Console.WriteLine("Quitting...");
                            break;
                    }
                }

                // Small delay to prevent CPU hogging
                await Task.Delay(10);
            }

            // Stop the emulator
            emulator.Stop();

            // Wait for the server task to complete
            await serverTask;

            Console.WriteLine("Zwift Play emulator stopped.");
        }

        private void RunKickrBIKEmDNS()
        {
            _mDNSServiceKICKRBike = new MulticastService();

            // Setup Event handlers for when the service is running.
            _mDNSServiceKICKRBike.QueryReceived += (s, e) =>
            {
                foreach (var question in e.Message.Questions)
                {
                    if ((question.Name == "_wahoo-fitness-tnp._tcp.local") && (question.Type == DnsType.PTR))
                    {
                        //System.Diagnostics.Debug.WriteLine($"Received query from {e.RemoteEndPoint.Address}");
                        _zwiftIPAddress = e.RemoteEndPoint.Address;
                        _zwiftPort = e.RemoteEndPoint.Port;

                        // Answer in this order
                        var response = new Message();
                        response.Answers.Add(new PTRRecord
                        {
                            Name = "_wahoo-fitness-tnp._tcp.local",
                            DomainName = "KICKR Bike._wahoo-fitness-tnp._tcp.local",
                            Class = DnsClass.IN,
                            Type = DnsType.PTR,
                            TTL = TimeSpan.FromSeconds(3600)
                        });

                        response.Answers.Add(new SRVRecord
                        {
                            Name = "KICKR Bike._wahoo-fitness-tnp._tcp.",
                            Target = "KICKR BikeH.local.",
                            Port = 36866,
                            Priority = 0,
                            Weight = 0,
                            TTL = TimeSpan.FromSeconds(3600),
                        });

                        response.Answers.Add(new ARecord
                        {
                            Name = "KICKR BikeH.local",
                            Address = e.RemoteEndPoint.Address,
                            Class = DnsClass.IN,
                            TTL = TimeSpan.FromSeconds(3600),
                        });

                        response.Answers.Add(new TXTRecord
                        {
                            Name = "KICKR Bike._wahoo-fitness-tnp._tcp.local",
                            Type = DnsType.TXT,
                            Class = DnsClass.IN,
                            Strings = new List<string>
                            {
                                "ble-service-uuids=0x1818,0x1826",
                                "mac-address=B4:03:19:00:05:05",
                                "serial-number=240117"
                            },
                            TTL = TimeSpan.FromSeconds(3600)
                        });

                        _mDNSServiceKICKRBike.SendAnswer(response);
                    }
                }
            };

            var serviceDiscovery = new ServiceDiscovery(_mDNSServiceKICKRBike);
            
            var kickrBikeProfile = new ServiceProfile("KICKR Bike", "_wahoo-fitness-tnp._tcp.local", 36866);
            kickrBikeProfile.AddProperty("serial-number", "240117");
            kickrBikeProfile.AddProperty("mac-address", "B4-03-19-00-05-05");
            kickrBikeProfile.AddProperty("ble-service-uuids", "0x1818,0x1826");
            serviceDiscovery.Advertise(kickrBikeProfile);

            _mDNSServiceKICKRBike.Start();
        }

        private TcpListener? _listener;

        private async Task RunListener()
        {
            while (true)
            {
                _listener = new TcpListener(IPAddress.Any, 36866);
                _listener.Start();
                try
                {
                    while (true)
                    {
                        TcpClient tcpClient = _listener.AcceptTcpClient();
                        await ProcessClientBleBackend(tcpClient);
                    }
                }
                catch (Exception ex)
                {
                    
                }
            }
        }

        private async Task ProcessClientBleBackend(TcpClient client)
        {
            //_outboundMessages.Clear();
            //TcpClient client = (TcpClient)state;
            NetworkStream stream = client.GetStream();
            //try
            //{
            //    while (true)
            //    {
            //        if (_outboundMessages.TryDequeue(out var result))
            //        {
            //            Send(stream, result.MessageIdentifier, result.Sequence, result.MessageResponse, result.Data);
            //            continue;
            //        }
            //        if (!stream.Socket.Connected)
            //        {
            //            break;
            //        }
            //        if (!stream.DataAvailable)
            //        {
            //            Thread.Sleep(50);
            //            continue;
            //        }
            //        DirconMessage msg = ReadMessage(stream);
            //        if (msg.MessageIdentifier == DirconMessageIdentifier.DiscoverServices)
            //        {
            //            _log.Debug("Dircon:DS:DiscoverServices");
            //            List<byte> list = new List<byte>();
            //            foreach (GattDeviceService bleService in _bleServices)
            //            {
            //                list.AddRange(GuidToNetworkBytes(bleService.Uuid));
            //            }
            //            msg.MessageResponse = DirconMessageResponse.Success;
            //            msg.Data = list.ToArray();
            //            QueueOutboundMessage(msg);
            //        }
            //        else if (msg.MessageIdentifier == DirconMessageIdentifier.DiscoverCharacteristics)
            //        {
            //            _log.Debug("Dircon:DC: " + BitConverter.ToString(msg.Data).Replace("-", string.Empty));
            //            if (msg.Data.Length != 16)
            //            {
            //                continue;
            //            }
            //            List<byte> list2 = new List<byte>();
            //            list2.AddRange(msg.Data);
            //            Guid serviceGuid = new Guid(msg.Data, bigEndian: true);
            //            foreach (Tuple<Guid, Guid> item in _bleGGToCharacteristic.Keys.Where((Tuple<Guid, Guid> e) => e.Item1 == serviceGuid))
            //            {
            //                if (_bleGuidToGattCharacteristic.TryGetValue(item.Item2, out GattCharacteristic value) && value != null)
            //                {
            //                    list2.AddRange(GuidToNetworkBytes(value.Uuid));
            //                    byte b = 0;
            //                    if (value.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Read))
            //                    {
            //                        b |= 1;
            //                    }
            //                    if (value.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Write) || value.CharacteristicProperties.HasFlag(GattCharacteristicProperties.WriteWithoutResponse))
            //                    {
            //                        b |= 2;
            //                    }
            //                    if (value.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify) || value.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Indicate))
            //                    {
            //                        b |= 4;
            //                    }
            //                    list2.Add(b);
            //                }
            //            }
            //            msg.Data = list2.ToArray();
            //            msg.MessageResponse = DirconMessageResponse.Success;
            //            QueueOutboundMessage(msg);
            //        }
            //        else if (msg.MessageIdentifier == DirconMessageIdentifier.WriteCharacteristic)
            //        {
            //            try
            //            {
            //                if (msg.Data.Length > 16)
            //                {
            //                    List<byte> data = new List<byte>();
            //                    data.AddRange(msg.Data.Take(16).ToArray());
            //                    Guid guid = new Guid(data.ToArray(), bigEndian: true);
            //                    byte[] array = msg.Data.Skip(16).ToArray();
            //                    _log.Debug($"Dircon:WR: {guid} {BitConverter.ToString(array).Replace("-", string.Empty)}");
            //                    if (_bleGuidToGattCharacteristic.TryGetValue(guid, out GattCharacteristic value2) && value2 != null)
            //                    {
            //                        await value2.WriteValueAsync(array.AsBuffer());
            //                    }
            //                    msg.Data = data.ToArray();
            //                    msg.MessageResponse = DirconMessageResponse.Success;
            //                    QueueOutboundMessage(msg);
            //                }
            //            }
            //            catch (Exception ex)
            //            {
            //                _log.Error("Dircon:WR: Write Characteristic failed: " + ex.Message);
            //            }
            //        }
            //        else if (msg.MessageIdentifier == DirconMessageIdentifier.ReadCharacteristic)
            //        {
            //            try
            //            {
            //                if (msg.Data.Length != 16)
            //                {
            //                    continue;
            //                }
            //                List<byte> data = new List<byte>();
            //                data.AddRange(msg.Data);
            //                Guid guid2 = new Guid(data.ToArray(), bigEndian: true);
            //                _log.Debug($"Dircon:RD: {guid2}");
            //                msg.MessageResponse = DirconMessageResponse.Error;
            //                if (_bleGuidToGattCharacteristic.TryGetValue(guid2, out GattCharacteristic value3))
            //                {
            //                    if (value3 != null && value3.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Read))
            //                    {
            //                        GattReadResult gattReadResult = await value3.ReadValueAsync();
            //                        if (gattReadResult.Status == GattCommunicationStatus.Success)
            //                        {
            //                            msg.MessageResponse = DirconMessageResponse.Success;
            //                            data.AddRange(gattReadResult.Value.ToArray());
            //                        }
            //                    }
            //                }
            //                else
            //                {
            //                    msg.MessageResponse = DirconMessageResponse.CharacteristicNotFound;
            //                }
            //                msg.Data = data.ToArray();
            //                QueueOutboundMessage(msg);
            //            }
            //            catch (Exception ex2)
            //            {
            //                _log.Error("Dircon:RD: Read Characteristic failed: " + ex2.Message);
            //            }
            //        }
            //        else
            //        {
            //            if (msg.MessageIdentifier != DirconMessageIdentifier.EnableCharacteristicNotifications || msg.Data.Length != 17)
            //            {
            //                continue;
            //            }
            //            List<byte> data = new List<byte>();
            //            data.AddRange(msg.Data.Take(16).ToArray());
            //            Guid guid3 = new Guid(data.ToArray(), bigEndian: true);
            //            bool flag = msg.Data[16] != 0;
            //            _log.Debug($"Dircon:EN: {guid3} {flag}");
            //            msg.MessageResponse = DirconMessageResponse.Error;
            //            if (_bleGuidToGattCharacteristic.TryGetValue(guid3, out GattCharacteristic gc))
            //            {
            //                if (gc != null && (gc.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify) || gc.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Indicate)))
            //                {
            //                    if (flag)
            //                    {
            //                        if (_bleNotifyEnabled.TryGetValue(guid3, out var value4))
            //                        {
            //                            if (value4)
            //                            {
            //                                msg.MessageResponse = DirconMessageResponse.Success;
            //                                _dirconNotifyForward[guid3] = true;
            //                            }
            //                            else
            //                            {
            //                                GattClientCharacteristicConfigurationDescriptorValue clientCharacteristicConfigurationDescriptorValue = GattClientCharacteristicConfigurationDescriptorValue.None;
            //                                if (gc.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Indicate))
            //                                {
            //                                    clientCharacteristicConfigurationDescriptorValue = GattClientCharacteristicConfigurationDescriptorValue.Indicate;
            //                                }
            //                                if (gc.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
            //                                {
            //                                    clientCharacteristicConfigurationDescriptorValue = GattClientCharacteristicConfigurationDescriptorValue.Notify;
            //                                }
            //                                gc.ValueChanged += BleValueChanged;
            //                                _dirconNotifyForward[guid3] = true;
            //                                _bleNotifyEnabled[guid3] = true;
            //                                if (await gc.WriteClientCharacteristicConfigurationDescriptorAsync(clientCharacteristicConfigurationDescriptorValue) == GattCommunicationStatus.Success)
            //                                {
            //                                    msg.MessageResponse = DirconMessageResponse.Success;
            //                                }
            //                                else
            //                                {
            //                                    _log.Error($"Unable to enable ble notification on {gc.Uuid}");
            //                                    msg.MessageResponse = DirconMessageResponse.Error;
            //                                }
            //                            }
            //                        }
            //                        else
            //                        {
            //                            _log.Error("Logic error when enabling ble notification");
            //                            msg.MessageResponse = DirconMessageResponse.Error;
            //                        }
            //                    }
            //                    else
            //                    {
            //                        _dirconNotifyForward[guid3] = false;
            //                        msg.MessageResponse = DirconMessageResponse.Success;
            //                    }
            //                }
            //                else
            //                {
            //                    msg.MessageResponse = DirconMessageResponse.Error;
            //                }
            //            }
            //            else
            //            {
            //                msg.MessageResponse = DirconMessageResponse.CharacteristicNotFound;
            //            }
            //            msg.Data = data.ToArray();
            //            QueueOutboundMessage(msg);
            //            gc = null;
            //        }
            //    }
            //    throw new Exception("Socket is not connected");
            //}
            //catch (Exception ex3)
            //{
            //    _log.Error("Dircon Server Exception " + ex3.Message);
            //}
            //_log.Info("Dircon Server Shutdown");
            //foreach (Guid key in _dirconNotifyForward.Keys)
            //{
            //    _dirconNotifyForward[key] = false;
            //}
            //_outboundMessages.Clear();
            client.Dispose();
        }

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
                        //System.Diagnostics.Debug.WriteLine($"Received query from {e.RemoteEndPoint.Address}");
                        _zwiftIPAddress = e.RemoteEndPoint.Address;
                        _zwiftPort = e.RemoteEndPoint.Port;

                        // Answer in this order
                        var response = new Message();
                        //response.Answers.Add(new PTRRecord
                        //{
                        //    Name = "_wahoo-fitness-tnp._tcp.local",
                        //    DomainName = "KICKR BIKE SHIFT B84D._wahoo-fitness-tnp._tcp.local",
                        //    Class = DnsClass.IN,
                        //    Type = DnsType.PTR,
                        //    TTL = TimeSpan.FromSeconds(3600)
                        //});

                        //response.Answers.Add(new SRVRecord
                        //{
                        //    //service: _wahoo-fitness-tnp
                        //    // instance: Wahoo KICKR
                        //    Name = "KICKR BIKE SHIFT B84D._wahoo-fitness-tnp._tcp.",
                        //    Target = "KICKR BIKE SHIFT B84DH.local.",
                        //    Port = 36866,
                        //    Priority = 0,
                        //    Weight = 0,
                        //    TTL = TimeSpan.FromSeconds(3600),
                        //});

                        //response.Answers.Add(new ARecord
                        //{
                        //    Name = "KICKR BIKE SHIFT B84DH.local",
                        //    Address = e.RemoteEndPoint.Address,
                        //    Class = DnsClass.IN,
                        //    TTL = TimeSpan.FromSeconds(3600),
                        //});

                        response.Answers.Add(new TXTRecord
                        {
                            //Name = "KICKR BIKE SHIFT B84D._wahoo-fitness-tnp._tcp.local",
                            Name = "Voltage Controller._wahoo-fitness-tnp._tcp.local",
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

            //var wahooServiceProfile = new ServiceProfile("KICKR BIKE SHIFT B84D", "_wahoo-fitness-tnp._tcp.local", 36866);
            var wahooServiceProfile = new ServiceProfile("Voltage Controller", "_wahoo-fitness-tnp._tcp.local", 36866);

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
