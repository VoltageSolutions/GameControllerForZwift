//using System;
//using System.IO;
//using System.Net;
//using System.Net.Sockets;
//using System.Text;
//using System.Text.Json;
//using System.Threading;
//using System.Threading.Tasks;

//namespace GameControllerForZwift.Network.IntegrationTests.copilotv1

//public class ZwiftControllerEmulator
//{
//    private UdpClient udpClient;
//    private IPEndPoint zwiftEndpoint;
//    private CancellationTokenSource cancellationTokenSource;
//    private ZwiftControllerState currentState;
//    private MdnsAdvertiser mdnsAdvertiser;

//    public ZwiftControllerEmulator()
//    {
//        udpClient = new UdpClient(12345); // Listen on port 12345 for incoming connections
//        currentState = new ZwiftControllerState();
//        mdnsAdvertiser = new MdnsAdvertiser("ZwiftPlay", "_zwiftplay._udp", 12345);
//    }

//    public void Start()
//    {
//        cancellationTokenSource = new CancellationTokenSource();
//        mdnsAdvertiser.Start();
//        Task.Run(() => ListenForZwiftConnections(cancellationTokenSource.Token));
//        Task.Run(() => StreamData(cancellationTokenSource.Token));
//    }

//    public void Stop()
//    {
//        cancellationTokenSource?.Cancel();
//        udpClient?.Close();
//        mdnsAdvertiser.Stop();
//    }

//    public void UpdateState(ZwiftControllerState newState)
//    {
//        currentState = newState;
//    }

//    private async Task ListenForZwiftConnections(CancellationToken cancellationToken)
//    {
//        while (!cancellationToken.IsCancellationRequested)
//        {
//            try
//            {
//                var result = await udpClient.ReceiveAsync();
//                zwiftEndpoint = result.RemoteEndPoint; // Set the endpoint based on incoming connection
//                Console.WriteLine($"Zwift connected from {zwiftEndpoint}");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error receiving data: {ex.Message}");
//            }
//        }
//    }

//    private async Task StreamData(CancellationToken cancellationToken)
//    {
//        while (!cancellationToken.IsCancellationRequested)
//        {
//            if (zwiftEndpoint == null)
//            {
//                await Task.Delay(100); // Wait for a connection
//                continue;
//            }

//            try
//            {
//                var message = new ControllerStateMessage
//                {
//                    LeftButton = currentState.LeftButton,
//                    RightButton = currentState.RightButton,
//                    UpButton = currentState.UpButton,
//                    DownButton = currentState.DownButton,
//                    AButton = currentState.AButton,
//                    BButton = currentState.BButton,
//                    AnalogLeftRight = currentState.AnalogLeftRight,
//                    AnalogUpDown = currentState.AnalogUpDown
//                };

//                var data = message.ToByteArray();
//                await udpClient.SendAsync(data, data.Length, zwiftEndpoint);
//                await Task.Delay(100); // Stream at 10 Hz
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error streaming data: {ex.Message}");
//            }
//        }
//    }
//}

//public class ZwiftControllerState
//{
//    public bool LeftButton { get; set; }
//    public bool RightButton { get; set; }
//    public bool UpButton { get; set; }
//    public bool DownButton { get; set; }
//    public bool AButton { get; set; }
//    public bool BButton { get; set; }
//    public int AnalogLeftRight { get; set; } // -100 to 100
//    public int AnalogUpDown { get; set; } // -100 to 100
//}

//public class MdnsAdvertiser
//{
//    private string serviceName;
//    private string serviceType;
//    private int port;

//    public MdnsAdvertiser(string serviceName, string serviceType, int port)
//    {
//        this.serviceName = serviceName;
//        this.serviceType = serviceType;
//        this.port = port;
//    }

//    public void Start()
//    {
//        // Implement mDNS advertisement logic here
//        Console.WriteLine($"Advertising {serviceName} on {serviceType} at port {port}");
//    }

//    public void Stop()
//    {
//        // Stop mDNS advertisement
//        Console.WriteLine($"Stopping advertisement for {serviceName}");
//    }
//}

//// Define the Protobuf message for controller state
//public class ControllerStateMessage : IMessage<ControllerStateMessage>
//{
//    public bool LeftButton { get; set; }
//    public bool RightButton { get; set; }
//    public bool UpButton { get; set; }
//    public bool DownButton { get; set; }
//    public bool AButton { get; set; }
//    public bool BButton { get; set; }
//    public int AnalogLeftRight { get; set; } // -100 to 100
//    public int AnalogUpDown { get; set; } // -100 to 100

//    // Protobuf serialization methods
//    public void MergeFrom(CodedInputStream input)
//    {
//        // Implement deserialization logic here
//    }

//    public void WriteTo(CodedOutputStream output)
//    {
//        // Implement serialization logic here
//    }

//    public int CalculateSize()
//    {
//        // Implement size calculation logic here
//        return 0;
//    }

//    public MessageDescriptor Descriptor => null; // Replace with actual descriptor if needed

//    public void MergeFrom(ControllerStateMessage message)
//    {
//        // Implement merging logic here
//    }

//    public bool Equals(ControllerStateMessage other)
//    {
//        // Implement equality logic here
//        return false;
//    }
//}