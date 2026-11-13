using GameControllerForZwift.Network.Dircon;
using GameControllerForZwift.Network.Mdns;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace GameControllerForZwift.Network.IntegrationTests
{
    public class MdnsAdvertiserTests
    {
        [Fact]
        public void GetAdvertisedAddresses_ReturnsOnlyOwnUsableIPv4Addresses()
        {
            foreach (var address in MdnsAdvertiser.GetAdvertisedAddresses())
            {
                Assert.Equal(AddressFamily.InterNetwork, address.AddressFamily);
                Assert.False(IPAddress.IsLoopback(address));
                Assert.False(address.GetAddressBytes() is [169, 254, ..]);
            }
        }

        [Fact]
        public async Task Start_AnnouncesWahooServiceOnMulticast()
        {
            const int mdnsPort = 5353;
            using var client = new UdpClient(AddressFamily.InterNetwork);
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.ExclusiveAddressUse = false;
            client.Client.Bind(new IPEndPoint(IPAddress.Any, mdnsPort));
            client.JoinMulticastGroup(IPAddress.Parse("224.0.0.251"));

            await using var advertiser = new MdnsAdvertiser();
            advertiser.Start(DeviceIdentity.Default, 36866);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            var found = false;
            while (!cts.IsCancellationRequested && !found)
            {
                var receive = client.ReceiveAsync();
                var done = await Task.WhenAny(receive, Task.Delay(Timeout.Infinite, cts.Token).ContinueWith(_ => { }));
                if (done != receive) break;
                var text = Encoding.ASCII.GetString(receive.Result.Buffer);
                found = text.Contains("_wahoo-fitness-tnp", StringComparison.OrdinalIgnoreCase);
            }

            Assert.True(found, "Did not observe an mDNS advertisement for _wahoo-fitness-tnp._tcp.");
        }
    }
}
