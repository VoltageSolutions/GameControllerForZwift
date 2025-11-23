using Makaretu.Dns;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GameControllerForZwift.Network
{
    public class MulticastDNSHelper : IDisposable
    {
        #region Fields
        //private readonly ILogger<MulticastDNSHelper> _logger;
        private MulticastService? _mDNSControllerService;

        // Should these be configurable?
        public const string ControllerName = "KICKR BIKE Voltage";
        public const string HostDomain = "_wahoo-fitness-tnp._tcp.local";
        private const ushort Port = 36866;
        private const string BLEServiceUUIDs = "ble-service-uuids=0xFC82,0x1818,0x1826,00000001-19CA-4651-86E5-FA29DCDD09D1,A026EE0D-0A7D-4AB3-97FA-F1500F9FEB8B";
        // todo - can we randomize these?
        private const string MACAddress = "mac-address=68-67-25-6C-66-9C";
        private const string SerialNumber = "serial-number=234700181";
        #endregion

        #region Constructor
        public MulticastDNSHelper()
        {
            // todo - bubble up messages (events?) for parent to use for logging
            //_logger = logger;
            AdvertiseControllerService();
        }

        #endregion

        #region Methods
        public void AdvertiseControllerService()
        {
            _mDNSControllerService = new MulticastService();

            // Setup Event handlers for when the service is running.
            _mDNSControllerService.QueryReceived += (s, e) =>
            {
                foreach (var question in e.Message.Questions)
                {
                    if ((question.Name == HostDomain) && (question.Type == DnsType.PTR))
                    {
                        //_logger.LogDebug($"Received query from {e.RemoteEndPoint.Address}");
                        System.Diagnostics.Debug.WriteLine($"Received query from {e.RemoteEndPoint.Address}");

                        // Answer in this order
                        var response = new Message();
                        response.Answers.Add(new PTRRecord
                        {
                            Name = HostDomain,
                            // Connecting to Zwift seems to only work when this has the correct KICKR name.
                            DomainName = string.Concat(ControllerName, ".", HostDomain),
                            Class = DnsClass.IN,
                            Type = DnsType.PTR,
                            //TTL = TimeSpan.FromSeconds(4500)
                            TTL = TimeSpan.FromSeconds(60)
                        });

                        response.Answers.Add(new SRVRecord
                        {
                            Name = string.Concat(ControllerName, ".", HostDomain),
                            Target = string.Concat(ControllerName, ".local."),
                            Port = Port,
                            Priority = 0,
                            Weight = 0,
                            //TTL = TimeSpan.FromSeconds(4500)
                            TTL = TimeSpan.FromSeconds(60)
                        });

                        response.Answers.Add(new ARecord
                        {
                            Name = string.Concat(ControllerName, ".local."),
                            Address = e.RemoteEndPoint.Address,
                            Class = DnsClass.IN,
                            //TTL = TimeSpan.FromSeconds(3600)
                            TTL = TimeSpan.FromSeconds(60)
                        });

                        response.Answers.Add(new TXTRecord
                        {
                            Name = string.Concat(ControllerName, ".", HostDomain),
                            Type = DnsType.TXT,
                            Class = DnsClass.IN,
                            Strings = new List<string>
                            {
                                BLEServiceUUIDs,
                                MACAddress,
                                SerialNumber
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
                        //_logger.LogDebug($"Responded to query from {e.RemoteEndPoint.Address}");
                        System.Diagnostics.Debug.WriteLine($"Responded to query from {e.RemoteEndPoint.Address}");
                    }
                }
            };

            var serviceDiscovery = new ServiceDiscovery(_mDNSControllerService);
            _mDNSControllerService.Start();

            var controllerServiceProfile = new ServiceProfile(ControllerName, HostDomain, Port);

            // Make sure this service is unique / doesn't already exist on the netowrk
            if (!serviceDiscovery.Probe(controllerServiceProfile))
            {
                serviceDiscovery.Advertise(controllerServiceProfile);
                serviceDiscovery.Announce(controllerServiceProfile);
            }
        }

        public void Dispose()
        {
            _mDNSControllerService?.Stop();
            _mDNSControllerService?.Dispose();
        }
        #endregion
    }
}