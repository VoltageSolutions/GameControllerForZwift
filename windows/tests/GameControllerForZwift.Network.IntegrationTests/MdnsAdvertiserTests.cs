using GameControllerForZwift.Network.Mdns;
using System.Net;
using System.Net.Sockets;

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
    }
}
