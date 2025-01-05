using Makaretu.Dns;
using System.Net;

namespace GameControllerForZwift.Network.IntegrationTests
{
    public class UnitTest1
    {
        private MulticastService? _mdnsService;
        private IPAddress? _zwiftIPAddress;
        private int? _zwiftPort;

        [Fact]
        public void Test1()
        {
            RunWahooKICKRmDNS();


            Thread.Sleep(3000);
            System.Diagnostics.Debug.WriteLine($"Zwift IP Address: {_zwiftIPAddress}");
            System.Diagnostics.Debug.WriteLine($"Zwift Port: {_zwiftPort}");

            /*
            var client = new TcpClient();
            await client.ConnectAsync(ipAddress, port); // Use the appropriate port
            using var stream = client.GetStream();
            var message = Encoding.UTF8.GetBytes("Hello from Wahoo KICKR");
            await stream.WriteAsync(message, 0, message.Length);
            */

            _mdnsService?.Stop();
            Assert.True(true);
        }

        private void RunWahooKICKRmDNS()
        {
            _mdnsService = new MulticastService();

            // Setup Event handlers for when the service is running.
            _mdnsService.QueryReceived += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"Query Received from Endpoint: {e.RemoteEndPoint.Address}");
                var names = e.Message.Questions
                    .Select(q => q.Name + " " + q.Type);

                System.Diagnostics.Debug.WriteLine("Queries received:");
                foreach (var name in names)
                    System.Diagnostics.Debug.WriteLine(name);


                foreach (var question in e.Message.Questions)
                {
                    if ((question.Name == "_wahoo-fitness-tnp._tcp.local") && (question.Type == DnsType.PTR))
                    {
                        System.Diagnostics.Debug.WriteLine($"Received query from {e.RemoteEndPoint.Address}")
                        ;
                        _zwiftIPAddress = e.RemoteEndPoint.Address;
                        _zwiftPort = e.RemoteEndPoint.Port;

                        // Answer in this order
                        var response = new Message();
                        response.Answers.Add(new PTRRecord
                        {
                            Name = "_wahoo-fitness-tnp._tcp.local",
                            DomainName = "Wahoo KICKR 0000._wahoo-fitness-tnp._tcp.local", 
                            Class = DnsClass.IN, 
                            Type = DnsType.PTR,
                            TTL = TimeSpan.FromSeconds(3600)
                        });

                        response.Answers.Add(new SRVRecord
                        {
                            //service: _wahoo-fitness-tnp
                            // instance: Wahoo KICKR 0000
                            Name = "Wahoo KICKR 0000._wahoo-fitness-tnp._tcp.",
                            Target = "Wahoo KICKR 0000H.local.",
                            Port = 36866,
                            Priority = 0,
                            Weight = 0,
                            TTL = TimeSpan.FromSeconds(3600),
                        });

                        response.Answers.Add(new ARecord
                        {
                            Name = "Wahoo KICKR 0000H.local",
                            Address = e.RemoteEndPoint.Address,
                            Class = DnsClass.IN,
                            TTL = TimeSpan.FromSeconds(3600),
                        });

                        response.Answers.Add(new TXTRecord
                        {
                            Name = "Wahoo KICKR 0000._wahoo-fitness-tnp._tcp.local",
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

                        _mdnsService.SendAnswer(response);
                        System.Diagnostics.Debug.WriteLine($"Responded to query from {e.RemoteEndPoint.Address}");
                    }
                }
            };
            _mdnsService.AnswerReceived += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"Answer Received from Endpoint: {e.RemoteEndPoint.Address}");
                var names = e.Message.Answers
                    .Select(q => q.Name + " " + q.Type)
                    .Distinct();

                System.Diagnostics.Debug.WriteLine("Answers received:");
                foreach (var name in names)
                    System.Diagnostics.Debug.WriteLine(name);
            };


            var serviceDiscovery = new ServiceDiscovery(_mdnsService);
            _mdnsService.Start();

            var wahooServiceProfile = new ServiceProfile("Wahoo KICKR 0000", "_wahoo-fitness-tnp._tcp.local", 36866);

            // Make sure this service is unique / doesn't already exist on the netowrk
            if (!serviceDiscovery.Probe(wahooServiceProfile))
            {
                serviceDiscovery.Advertise(wahooServiceProfile);
                serviceDiscovery.Announce(wahooServiceProfile);
            }
        }
    }
}
