using Castle.Components.DictionaryAdapter.Xml;
using Makaretu.Dns;
using System.Net;

namespace GameControllerForZwift.Network.IntegrationTests
{
    public class UnitTest1
    {
        private MulticastService? _mDNSServiceWahoo;
        private MulticastService? _mDNSServiceJetBlack;
        private IPAddress? _zwiftIPAddress;
        private int? _zwiftPort;

        [Fact]
        public void Test1()
        {
            //RunWahooKICKRmDNS();
            RunJetBlackVictorymDNS();


            Thread.Sleep(5000);
            System.Diagnostics.Debug.WriteLine($"Zwift IP Address: {_zwiftIPAddress}");
            System.Diagnostics.Debug.WriteLine($"Zwift Port: {_zwiftPort}");

            /*
            var client = new TcpClient();
            await client.ConnectAsync(ipAddress, port); // Use the appropriate port
            using var stream = client.GetStream();
            var message = Encoding.UTF8.GetBytes("Hello from Wahoo KICKR");
            await stream.WriteAsync(message, 0, message.Length);
            */

            _mDNSServiceWahoo?.Stop();
            _mDNSServiceJetBlack?.Stop();
            Assert.True(true);
        }

        private void RunWahooKICKRmDNS()
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
                        response.Answers.Add(new PTRRecord
                        {
                            Name = "_wahoo-fitness-tnp._tcp.local",
                            DomainName = "Wahoo KICKR._wahoo-fitness-tnp._tcp.local", 
                            Class = DnsClass.IN, 
                            Type = DnsType.PTR,
                            TTL = TimeSpan.FromSeconds(3600)
                        });

                        response.Answers.Add(new SRVRecord
                        {
                            //service: _wahoo-fitness-tnp
                            // instance: Wahoo KICKR
                            Name = "Wahoo KICKR._wahoo-fitness-tnp._tcp.",
                            Target = "Wahoo KICKRH.local.",
                            Port = 36866,
                            Priority = 0,
                            Weight = 0,
                            TTL = TimeSpan.FromSeconds(3600),
                        });

                        response.Answers.Add(new ARecord
                        {
                            Name = "Wahoo KICKRH.local",
                            Address = e.RemoteEndPoint.Address,
                            Class = DnsClass.IN,
                            TTL = TimeSpan.FromSeconds(3600),
                        });

                        response.Answers.Add(new TXTRecord
                        {
                            Name = "Wahoo KICKR._wahoo-fitness-tnp._tcp.local",
                            Type = DnsType.TXT,
                            Class = DnsClass.IN,
                            Strings = new List<string>
                            {
                                "ble-service-uuids=00001826-0000-1000-8000-00805F9B34FB,00001818-0000-1000-8000-00805F9B34FB,00001816-0000-1000-8000-00805F9B34FB",
                                "mac-address=A8:A1:59:EB:A0:41",
                                "serial-number=0"
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

            var wahooServiceProfile = new ServiceProfile("Wahoo KICKR", "_wahoo-fitness-tnp._tcp.local", 36866);

            // Make sure this service is unique / doesn't already exist on the netowrk
            if (!serviceDiscovery.Probe(wahooServiceProfile))
            {
                serviceDiscovery.Advertise(wahooServiceProfile);
                serviceDiscovery.Announce(wahooServiceProfile);
            }
        }

        private void RunJetBlackVictorymDNS()
        {
            _mDNSServiceJetBlack = new MulticastService();

            // Setup Event handlers for when the service is running.
            _mDNSServiceJetBlack.QueryReceived += (s, e) =>
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
                        //    Name = "_services._dns-sd._udp.local",
                        //    DomainName = "_wahoo-fitness-tnp._tcp.local",
                        //    Class = DnsClass.IN,
                        //    Type = DnsType.PTR,
                        //    TTL = TimeSpan.FromSeconds(3600)
                        //});
                        response.Answers.Add(new PTRRecord
                        {
                            Name = "_wahoo-fitness-tnp._tcp.local",
                            DomainName = "Victory._wahoo-fitness-tnp._tcp.local",
                            Class = DnsClass.IN,
                            Type = DnsType.PTR,
                            TTL = TimeSpan.FromSeconds(3600)
                        });


                        response.Answers.Add(new SRVRecord
                        {
                            Name = "Victory._wahoo-fitness-tnp._tcp.",
                            Target = "victoryH.local.",
                            Port = 36866,
                            Priority = 0,
                            Weight = 0,
                            TTL = TimeSpan.FromSeconds(3600),
                        });

                        response.Answers.Add(new ARecord
                        {
                            Name = "victoryH.local",
                            Address = e.RemoteEndPoint.Address,
                            Class = DnsClass.IN,
                            TTL = TimeSpan.FromSeconds(3600),
                        });

                        response.Answers.Add(new TXTRecord
                        {
                            Name = "Victory._wahoo-fitness-tnp._tcp.local",
                            Type = DnsType.TXT,
                            Class = DnsClass.IN,
                            Strings = new List<string>
                            {
                                "ble-service-uuids=0x1800,0x1801,0x1826,0x1816,0xfe59,0x180d,180a",
                                "mac-address=7c-2c-67-1d-4c-18"
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
                        //System.Diagnostics.Debug.WriteLine($"Responded to query from {e.RemoteEndPoint.Address}");
                    }
                }
            };
            _mDNSServiceJetBlack.AnswerReceived += (s, e) =>
            {
                //System.Diagnostics.Debug.WriteLine($"Answer Received from Endpoint: {e.RemoteEndPoint.Address}");
                //var names = e.Message.Answers
                //    .Select(q => q.Name + " " + q.Type)
                //    .Distinct();

                //System.Diagnostics.Debug.WriteLine("Answers received:");
                //foreach (var name in names)
                //    System.Diagnostics.Debug.WriteLine(name);
            };


            var serviceDiscovery = new ServiceDiscovery(_mDNSServiceJetBlack);
            _mDNSServiceJetBlack.Start();

            //var requests = new Message();
            //requests.Questions.Add(new Question
            //{
            //    Name = "Victory._wahoo-fitness-tnp._tcp.local",
            //    Type = DnsType.ANY,
            //    Class = DnsClass.IN
            //});
            //requests.Questions.Add(new Question
            //{
            //    Name = "victoryH.local.",
            //    Type = DnsType.ANY,
            //    Class = DnsClass.IN
            //});
            //requests.Questions.Add(new Question
            //{
            //    Name = "victory.local.",
            //    Type = DnsType.ANY,
            //    Class = DnsClass.IN
            //});
            ////_mDNSServiceJetBlack.SendQuery(requests);
            //_mDNSServiceJetBlack.SendQuery("Victory._wahoo-fitness-tnp._tcp.local", DnsClass.IN, DnsType.ANY);

            var victoryServiceProfile = new ServiceProfile("Victory", "_wahoo-fitness-tnp._tcp.local", 36866);

            // Make sure this service is unique / doesn't already exist on the netowrk
            if (!serviceDiscovery.Probe(victoryServiceProfile))
            {
                serviceDiscovery.Advertise(victoryServiceProfile);
                serviceDiscovery.Announce(victoryServiceProfile);
            }
        }
    }
}
