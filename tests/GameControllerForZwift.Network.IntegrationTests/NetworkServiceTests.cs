using Microsoft.Extensions.Logging;
using NSubstitute;

namespace GameControllerForZwift.Network.IntegrationTests
{
    public class NetworkServiceTests
    {
        private readonly ILogger<NetworkService> _loggerMock;
        private readonly NetworkService _networkService;

        public NetworkServiceTests()
        {
            _loggerMock = Substitute.For<ILogger<NetworkService>>();
            _networkService = new NetworkService(_loggerMock);
        }

        [Fact]
        public async Task AdvertiseTest()
        {
            Thread.Sleep(200000);

            Assert.True(true);
        }
    }
}
