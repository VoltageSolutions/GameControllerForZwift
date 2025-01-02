namespace GameControllerForZwift.Network.IntegrationTests
{
    public class UnitTest1
    {
        [Fact]
        public async void Test1()
        {
            var responder = new MdnsResponder();

            const string serviceName = "_wahoo-fitness-tnp";
            const string hostName = "Wahoo KICKR 0000";
            const int servicePort = 36866;
            const string txtRecords = "ble-service-uuids=00001826-0000-1000-8000-00805F9B34FB,mac-address=A8:A1:59:EB:A0:41,serial-number=0";

            // Start listening for mDNS queries and respond
            await responder.StartListeningAsync(serviceName, hostName, servicePort, txtRecords);

            Console.WriteLine("Press any key to stop...");
            Console.ReadKey();
            responder.Stop();
        }
    }
}
