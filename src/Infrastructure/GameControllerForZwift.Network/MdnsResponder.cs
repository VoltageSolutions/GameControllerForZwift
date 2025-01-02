using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GameControllerForZwift.Network
{
    public class MdnsResponder
    {
        private const string MulticastAddress = "224.0.0.251";
        private const int MulticastPort = 5353;
        private readonly UdpClient _udpClient;

        public MdnsResponder()
        {
            //_udpClient = new UdpClient(MulticastPort);
            //_udpClient.JoinMulticastGroup(IPAddress.Parse(MulticastAddress));

            var endpoint = new IPEndPoint(IPAddress.Any, MulticastPort);
            _udpClient = new UdpClient();

            // Set the option to allow multiple applications to use the same port
            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            // Bind to the mDNS port
            _udpClient.Client.Bind(endpoint);

            // Join the mDNS multicast group
            _udpClient.JoinMulticastGroup(IPAddress.Parse(MulticastAddress));
        }

        public async Task StartListeningAsync(string serviceName, string hostName, int port, string txtRecords)
        {
            System.Diagnostics.Debug.WriteLine($"Listening for mDNS queries for {serviceName}...");

            while (true)
            {
                var result = await _udpClient.ReceiveAsync();
                var message = Encoding.UTF8.GetString(result.Buffer);

                // Check if the query matches the desired service
                if (message.Contains(serviceName))
                {
                    System.Diagnostics.Debug.WriteLine($"Query received for {serviceName}. Sending response...");
                    var response = BuildMdnsResponse(serviceName, hostName, port, txtRecords);
                    await _udpClient.SendAsync(response, response.Length, result.RemoteEndPoint);
                }
            }
        }

        private byte[] BuildMdnsResponse(string serviceName, string hostName, int port, string txtRecords)
        {
            var response = new StringBuilder();

            // Construct PTR Record
            response.Append($"{serviceName}._tcp.local.");
            response.Append($"PTR {hostName}._tcp.local.");
            
            // Construct SRV Record
            response.Append($"{hostName}._tcp.local.");
            response.Append($"SRV {port}.");
            
            // Construct TXT Records
            response.Append($"TXT {txtRecords}.");
            

            System.Diagnostics.Debug.WriteLine($"{serviceName}._tcp.local.");
            System.Diagnostics.Debug.WriteLine($"PTR {hostName}._tcp.local.");
            System.Diagnostics.Debug.WriteLine($"{hostName}._tcp.local.");
            System.Diagnostics.Debug.WriteLine($"SRV {port}._tcp.local.");
            System.Diagnostics.Debug.WriteLine($"TXT {txtRecords}._tcp.local.");

            //System.Diagnostics.Debug.WriteLine(Encoding.UTF8.GetBytes(response.ToString()));

            return Encoding.UTF8.GetBytes(response.ToString());
        }

        public void Stop()
        {
            _udpClient.DropMulticastGroup(IPAddress.Parse(MulticastAddress));
            _udpClient.Close();
            System.Diagnostics.Debug.WriteLine("Stopped listening for mDNS queries.");
        }
    }

}
