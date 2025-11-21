using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Makaretu.Dns;

namespace GameControllerForZwift.Network.IntegrationTests.copilotv3
{
    public class ZwiftPlayDevice
    {
        private MulticastService _mDNSService;
        private string _deviceName;
        private string _macAddress;
        private string _serialNumber;
        private int _port;
        private IPAddress _zwiftIPAddress;
        private int _zwiftPort;

        public ZwiftPlayDevice(string deviceName, string macAddress, string serialNumber, int port)
        {
            _deviceName = deviceName;
            _macAddress = macAddress;
            _serialNumber = serialNumber;
            _port = port;
        }

        public void Start()
        {
            _mDNSService = new MulticastService();

            _mDNSService.QueryReceived += (s, e) =>
            {
                foreach (var question in e.Message.Questions)
                {
                    if ((question.Name == "_wahoo-fitness-tnp._tcp.local") && (question.Type == DnsType.PTR))
                    {
                        _zwiftIPAddress = e.RemoteEndPoint.Address;
                        _zwiftPort = e.RemoteEndPoint.Port;

                        var response = new Message();
                        response.Answers.Add(new PTRRecord
                        {
                            Name = "_wahoo-fitness-tnp._tcp.local",
                            DomainName = $"{_deviceName}._wahoo-fitness-tnp._tcp.local",
                            Class = DnsClass.IN,
                            Type = DnsType.PTR,
                            TTL = TimeSpan.FromSeconds(3600)
                        });

                        response.Answers.Add(new SRVRecord
                        {
                            Name = $"{_deviceName}._wahoo-fitness-tnp._tcp.local",
                            Target = $"{_deviceName}H.local.",
                            Port = (ushort)_port,
                            Priority = 0,
                            Weight = 0,
                            TTL = TimeSpan.FromSeconds(3600),
                        });

                        response.Answers.Add(new ARecord
                        {
                            Name = $"{_deviceName}H.local",
                            Address = e.RemoteEndPoint.Address,
                            Class = DnsClass.IN,
                            TTL = TimeSpan.FromSeconds(3600),
                        });

                        response.Answers.Add(new TXTRecord
                        {
                            Name = $"{_deviceName}._wahoo-fitness-tnp._tcp.local",
                            Type = DnsType.TXT,
                            Class = DnsClass.IN,
                            Strings = new List<string>
                        {
                            $"ble-service-uuids=0x1818,0x1826",
                            $"mac-address={_macAddress}",
                            $"serial-number={_serialNumber}"
                        },
                            TTL = TimeSpan.FromSeconds(3600)
                        });

                        _mDNSService.SendAnswer(response);
                    }
                }
            };

            var serviceDiscovery = new ServiceDiscovery(_mDNSService);

            var profile = new ServiceProfile(_deviceName, "_wahoo-fitness-tnp._tcp.local", (ushort)_port);
            profile.AddProperty("serial-number", _serialNumber);
            profile.AddProperty("mac-address", _macAddress);
            profile.AddProperty("ble-service-uuids", "0x1818,0x1826");
            serviceDiscovery.Advertise(profile);

            _mDNSService.Start();
        }

        public void Stop()
        {
            _mDNSService?.Dispose();
        }

        public void ProcessEncryptedData(byte[] encryptedData)
        {
            // Decrypt and process the data (placeholder for actual decryption logic)
            string decryptedData = Encoding.UTF8.GetString(encryptedData); // Example placeholder
            Console.WriteLine($"Decrypted Data: {decryptedData}");

            // Handle specific message types
            if (decryptedData.Contains("ButtonPress"))
            {
                Console.WriteLine("Button Press Detected");
            }
        }

        public void SendNotification(string message)
        {
            // Send a notification to Zwift (placeholder for actual implementation)
            Console.WriteLine($"Sending Notification: {message}");
        }
    }
}
