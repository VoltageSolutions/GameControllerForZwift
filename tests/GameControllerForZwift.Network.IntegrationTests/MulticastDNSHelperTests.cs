using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace GameControllerForZwift.Network.IntegrationTests
{
    public class MulticastDNSHelperTests
    {
        private readonly MulticastDNSHelper _multicastDNSHelper;

        public MulticastDNSHelperTests()
        {
            _multicastDNSHelper = new MulticastDNSHelper();
        }

        [Fact]
        public async Task AdvertiseTest()
        {
            // Listen for mDNS packets on the standard mDNS multicast address and port
            const string mdnsMulticast = "224.0.0.251";
            const int mdnsPort = 5353;
            const string expectedService =  "_wahoo-fitness-tnp._tcp.local";

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));

            // Start listening on UDP multicast for IPv4
            using var client = new UdpClient(AddressFamily.InterNetwork);
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.ExclusiveAddressUse = false;
            client.Client.Bind(new IPEndPoint(IPAddress.Any, mdnsPort));
            client.JoinMulticastGroup(IPAddress.Parse(mdnsMulticast));

            var receiveTask = Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var result = await client.ReceiveAsync().WithCancellation(cts.Token);
                        var bytes = result.Buffer;
                        // Look for the expected ASCII service string inside the packet
                        var text = Encoding.ASCII.GetString(bytes);
                        if (text.Contains(expectedService, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                    catch (OperationCanceledException) { break; }
                    catch { /* ignore parse/encoding errors and continue */ }
                }
                return false;
            });

            // Wait for either the packet or timeout
            var found = await receiveTask;

            Assert.True(found, "Did not observe mDNS advertisement for the expected service within the timeout.");
        }
    }

    static class TaskExtensions
    {
        public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            using var registration = cancellationToken.Register(() => { });
            var tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s!).TrySetResult(true), tcs))
            {
                if (task == await Task.WhenAny(task, tcs.Task).ConfigureAwait(false))
                {
                    return await task.ConfigureAwait(false);
                }
                throw new OperationCanceledException(cancellationToken);
            }
        }
    }
}
