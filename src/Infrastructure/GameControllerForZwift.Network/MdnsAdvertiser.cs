using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GameControllerForZwift.Network
{
    

    public class MdnsAdvertiser
    {
        private const string MulticastAddress = "224.0.0.251";
        private const int MulticastPort = 5353;
        private readonly UdpClient _udpClient;

        public MdnsAdvertiser()
        {
            _udpClient = new UdpClient
            {
                EnableBroadcast = true,
                MulticastLoopback = false
            };

            _udpClient.JoinMulticastGroup(IPAddress.Parse(MulticastAddress));
        }

        public async Task AdvertiseServiceAsync(string serviceName, int port, string txtRecord)
        {
            var message = BuildMdnsMessage(serviceName, port, txtRecord);
            var endpoint = new IPEndPoint(IPAddress.Parse(MulticastAddress), MulticastPort);

            System.Diagnostics.Debug.WriteLine($"Advertising service {serviceName} on port {port}...");

            while (true)
            {
                try
                {
                    await _udpClient.SendAsync(message, message.Length, endpoint);
                    await Task.Delay(TimeSpan.FromSeconds(1)); // Repeat every second
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error advertising service: {ex.Message}");
                    break;
                }
            }
        }

        private byte[] BuildMdnsMessage(string serviceName, int port, string txtRecord)
        {
            var message = new StringBuilder();

            // Add service name and domain
            message.Append($"{serviceName}._wahoo-fitness-tnp.local.");

            // Add SRV record
            message.Append($"SRV {port}.");

            // Add TXT records
            message.Append($"TXT {txtRecord}.");

            return Encoding.UTF8.GetBytes(message.ToString());
        }

        public void Stop()
        {
            _udpClient.DropMulticastGroup(IPAddress.Parse(MulticastAddress));
            _udpClient.Close();
        }
    }

}
