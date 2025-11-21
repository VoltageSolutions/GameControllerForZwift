//using Castle.Components.DictionaryAdapter.Xml;
//using Common.Logging;
//using InTheHand.Bluetooth;
//using Makaretu.Dns;
//using System.Diagnostics;
//using System.Net;
//using System.Net.Sockets;
//using System.Text;

//namespace GameControllerForZwift.Network.IntegrationTests
//{
//    public class UnitTest2
//    {

//        private MulticastService? _mDNSServiceJetBlack;

//        private IPAddress? _zwiftIPAddress;
//        private int? _zwiftPort;

//        [Fact]
//        public async Task Test1()
//        {
//            //RunJetBlackVoltagemDNS();
//            Main();

//            Thread.Sleep(10000);
//            System.Diagnostics.Debug.WriteLine($"Zwift IP Address: {_zwiftIPAddress}");
//            System.Diagnostics.Debug.WriteLine($"Zwift Port: {_zwiftPort}");

//            //_mDNSServiceJetBlack?.Stop();
//            Assert.True(true);
//        }

//        public async Task ZwiftPlayEmulatorTask()
//        {
//            // Create and start the emulator
//            using var emulator = new ZwiftPlayEmulator();

//            // Start the emulator in a background task
//            var serverTask = emulator.StartAsync();

//            Console.WriteLine("Zwift Play emulator started. Press keys to control:");
//            Console.WriteLine("A, B, Y, Z - Action buttons");
//            Console.WriteLine("Arrow keys - Direction buttons");
//            Console.WriteLine("L, R - Side buttons (LS, RS)");
//            Console.WriteLine("1-9 - Set analog left (ZL)");
//            Console.WriteLine("Shift+1-9 - Set analog right (ZR)");
//            Console.WriteLine("Q - Quit");

//            // Process keyboard input to control the emulator
//            bool running = true;
//            while (running)
//            {
//                if (Console.KeyAvailable)
//                {
//                    var key = Console.ReadKey(true);

//                    switch (key.Key)
//                    {
//                        case ConsoleKey.A:
//                            await emulator.ProcessButtonPressAsync(ControllerButton.A, true);
//                            await Task.Delay(100);
//                            await emulator.ProcessButtonPressAsync(ControllerButton.A, false);
//                            Console.WriteLine("Button A pressed");
//                            break;
//                        case ConsoleKey.B:
//                            await emulator.ProcessButtonPressAsync(ControllerButton.B, true);
//                            await Task.Delay(100);
//                            await emulator.ProcessButtonPressAsync(ControllerButton.B, false);
//                            Console.WriteLine("Button B pressed");
//                            break;
//                        case ConsoleKey.Y:
//                            await emulator.ProcessButtonPressAsync(ControllerButton.Y, true);
//                            await Task.Delay(100);
//                            await emulator.ProcessButtonPressAsync(ControllerButton.Y, false);
//                            Console.WriteLine("Button Y pressed");
//                            break;
//                        case ConsoleKey.Z:
//                            await emulator.ProcessButtonPressAsync(ControllerButton.Z, true);
//                            await Task.Delay(100);
//                            await emulator.ProcessButtonPressAsync(ControllerButton.Z, false);
//                            Console.WriteLine("Button Z pressed");
//                            break;
//                        case ConsoleKey.UpArrow:
//                            await emulator.ProcessButtonPressAsync(ControllerButton.Up, true);
//                            await Task.Delay(100);
//                            await emulator.ProcessButtonPressAsync(ControllerButton.Up, false);
//                            Console.WriteLine("Up arrow pressed");
//                            break;
//                        case ConsoleKey.DownArrow:
//                            await emulator.ProcessButtonPressAsync(ControllerButton.Down, true);
//                            await Task.Delay(100);
//                            await emulator.ProcessButtonPressAsync(ControllerButton.Down, false);
//                            Console.WriteLine("Down arrow pressed");
//                            break;
//                        case ConsoleKey.LeftArrow:
//                            await emulator.ProcessButtonPressAsync(ControllerButton.Left, true);
//                            await Task.Delay(100);
//                            await emulator.ProcessButtonPressAsync(ControllerButton.Left, false);
//                            Console.WriteLine("Left arrow pressed");
//                            break;
//                        case ConsoleKey.RightArrow:
//                            await emulator.ProcessButtonPressAsync(ControllerButton.Right, true);
//                            await Task.Delay(100);
//                            await emulator.ProcessButtonPressAsync(ControllerButton.Right, false);
//                            Console.WriteLine("Right arrow pressed");
//                            break;
//                        case ConsoleKey.L:
//                            await emulator.ProcessButtonPressAsync(ControllerButton.LS, true);
//                            await Task.Delay(100);
//                            await emulator.ProcessButtonPressAsync(ControllerButton.LS, false);
//                            Console.WriteLine("LS button pressed");
//                            break;
//                        case ConsoleKey.R:
//                            await emulator.ProcessButtonPressAsync(ControllerButton.RS, true);
//                            await Task.Delay(100);
//                            await emulator.ProcessButtonPressAsync(ControllerButton.RS, false);
//                            Console.WriteLine("RS button pressed");
//                            break;
//                        case ConsoleKey.D1:
//                        case ConsoleKey.D2:
//                        case ConsoleKey.D3:
//                        case ConsoleKey.D4:
//                        case ConsoleKey.D5:
//                        case ConsoleKey.D6:
//                        case ConsoleKey.D7:
//                        case ConsoleKey.D8:
//                        case ConsoleKey.D9:
//                            // Set analog values (1-9 maps to values 0-255)
//                            byte value = (byte)(((int)key.Key - (int)ConsoleKey.D1) * 28);
//                            if (key.Modifiers.HasFlag(ConsoleModifiers.Shift))
//                            {
//                                // Shift+number sets right analog (ZR)
//                                await emulator.UpdateAnalogInputsAsync(emulator.State.AnalogLeft, value);
//                                Console.WriteLine($"Set analog right (ZR) to {value}");
//                            }
//                            else
//                            {
//                                // Number sets left analog (ZL)
//                                await emulator.UpdateAnalogInputsAsync(value, emulator.State.AnalogRight);
//                                Console.WriteLine($"Set analog left (ZL) to {value}");
//                            }
//                            break;
//                        case ConsoleKey.Add:
//                        case ConsoleKey.OemPlus:
//                            // Increase power
//                            emulator.Power = Math.Min(emulator.Power + 10, 1000);
//                            Console.WriteLine($"Power increased to {emulator.Power}W");
//                            break;
//                        case ConsoleKey.Subtract:
//                        case ConsoleKey.OemMinus:
//                            // Decrease power
//                            emulator.Power = Math.Max(emulator.Power - 10, 50);
//                            Console.WriteLine($"Power decreased to {emulator.Power}W");
//                            break;
//                        case ConsoleKey.PageUp:
//                            // Increase cadence
//                            emulator.Cadence = Math.Min(emulator.Cadence + 5, 150);
//                            Console.WriteLine($"Cadence increased to {emulator.Cadence}rpm");
//                            break;
//                        case ConsoleKey.PageDown:
//                            // Decrease cadence
//                            emulator.Cadence = Math.Max(emulator.Cadence - 5, 30);
//                            Console.WriteLine($"Cadence decreased to {emulator.Cadence}rpm");
//                            break;
//                        case ConsoleKey.Home:
//                            // Increase speed
//                            emulator.Speed = Math.Min(emulator.Speed + 1.0f, 60.0f);
//                            Console.WriteLine($"Speed increased to {emulator.Speed:F1}km/h");
//                            break;
//                        case ConsoleKey.End:
//                            // Decrease speed
//                            emulator.Speed = Math.Max(emulator.Speed - 1.0f, 5.0f);
//                            Console.WriteLine($"Speed decreased to {emulator.Speed:F1}km/h");
//                            break;
//                        case ConsoleKey.Q:
//                            running = false;
//                            Console.WriteLine("Quitting...");
//                            break;
//                    }
//                }

//                // Small delay to prevent CPU hogging
//                await Task.Delay(10);
//            }

//            // Stop the emulator
//            emulator.Stop();

//            // Wait for the server task to complete
//            await serverTask;

//            Console.WriteLine("Zwift Play emulator stopped.");
//        }

//        private void RunJetBlackVoltagemDNS()
//        {
//            _mDNSServiceJetBlack = new MulticastService();

//            // Setup Event handlers for when the service is running.
//            _mDNSServiceJetBlack.QueryReceived += (s, e) =>
//            {
//                foreach (var question in e.Message.Questions)
//                {
//                    if ((question.Name == "_googlecast._tcp.local") && (question.Type == DnsType.PTR))
//                    {
//                        _zwiftIPAddress = e.RemoteEndPoint.Address;
//                        _zwiftPort = e.RemoteEndPoint.Port;

//                        // Answer in this order
//                        var response = new Message();
//                        response.Answers.Add(new PTRRecord
//                        {
//                            Name = "_googlecast._tcp.local",
//                            DomainName = "Composer Speaker._googlecast._tcp.local",
//                            Class = DnsClass.IN,
//                            Type = DnsType.PTR,
//                            TTL = TimeSpan.FromSeconds(3600)
//                        });


//                        response.Answers.Add(new SRVRecord
//                        {
//                            Name = "Composer Speaker._googlecast._tcp.local.",
//                            Target = "Composer SpeakerH.local.",
//                            Port = 8009,
//                            Priority = 0,
//                            Weight = 0,
//                            TTL = TimeSpan.FromSeconds(3600),
//                        });

//                        response.Answers.Add(new ARecord
//                        {
//                            Name = "Composer Speaker.local",
//                            Address = e.RemoteEndPoint.Address,
//                            Class = DnsClass.IN,
//                            TTL = TimeSpan.FromSeconds(3600),
//                        });

//                        response.Answers.Add(new TXTRecord
//                        {
//                            Name = "Composer Speaker._googlecast._tcp.local",
//                            Type = DnsType.TXT,
//                            Class = DnsClass.IN,
//                            Strings = new List<string>
//                            {
//                                "id=a8a159eba041",
//                                "cd=a8a159eba041",
//                                "rm=",
//                                "ve=05",
//                                "md=Google Home Mini",
//                                "ic=/setup/icon.png",
//                                "fn=Composer Speaker",
//                                "ca=199172",
//                                "st=0",
//                                "bs=FA8FCA301F25",
//                                "nf=1",
//                                "rs="
//                            },
//                            TTL = TimeSpan.FromSeconds(3600)
//                        });
//                        /*
//                         * 0x1800 - GAP Service
//                         * 0x1801 - GATT Service
//                         * 0x1826 - Cycling Power Service
//                         * 0x1816 - Cycling Speed and Cadence Service
//                         * 0xfe59 - Wahoo Fitness Service
//                         * 0x180d - Heart Rate Service
//                         * 180a - Device Information Service
//                         */



//                        _mDNSServiceJetBlack.SendAnswer(response);
//                        System.Diagnostics.Debug.WriteLine($"Zwift IP Address: {_zwiftIPAddress}");
//                        System.Diagnostics.Debug.WriteLine($"Zwift Port: {_zwiftPort}");
//                    }
//                }
//            };
//            //_mDNSServiceJetBlack.AnswerReceived += (s, e) =>
//            //{
//            //};


//            var serviceDiscovery = new ServiceDiscovery(_mDNSServiceJetBlack);
//            _mDNSServiceJetBlack.Start();

//            var voltageServiceProfile = new ServiceProfile("_googlecast", "_googlecast._tcp.local", 8009);

//            // Make sure this service is unique / doesn't already exist on the netowrk
//            //if (!serviceDiscovery.Probe(voltageServiceProfile))
//            //{
//            serviceDiscovery.Advertise(voltageServiceProfile);
//            serviceDiscovery.Announce(voltageServiceProfile);
//            //}
//        }



//    }
//}
