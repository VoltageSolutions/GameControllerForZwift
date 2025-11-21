//using Makaretu.Dns;
//using System;
//using System.Net;
//using System.Net.Sockets;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using Zeroconf;

//namespace GameControllerForZwift.Network.IntegrationTests.copilotv2;

//public class ZwiftPlayEmulator
//{
//    private UdpClient udpClient;
//    private IPEndPoint zwiftEndpoint;
//    private CancellationTokenSource cancellationTokenSource;
//    private ZwiftControllerState currentState;
//    private IServiceRegister serviceRegister;

//    public ZwiftPlayEmulator()
//    {
//        udpClient = new UdpClient(12345); // Listen on port 12345 for incoming connections
//        currentState = new ZwiftControllerState();
//    }

//    public void Start()
//    {
//        cancellationTokenSource = new CancellationTokenSource();
//        AdvertiseService();
//        Task.Run(() => ListenForZwiftConnections(cancellationTokenSource.Token));
//        Task.Run(() => StreamData(cancellationTokenSource.Token));
//    }

//    public void Stop()
//    {
//        cancellationTokenSource?.Cancel();
//        udpClient?.Close();
//        serviceRegister?.Dispose();
//    }

//    public void UpdateState(ZwiftControllerState newState)
//    {
//        currentState = newState;
//    }

//    private void AdvertiseService()
//    {
//        serviceRegister = ZeroconfResolver.RegisterServiceAsync(new RegisterService
//        {
//            Name = "ZwiftPlay",
//            RegType = "_zwiftplay._udp",
//            Port = 12345
//        }).Result;

//        Console.WriteLine("ZwiftPlay service advertised over mDNS.");
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

//public class ControllerStateMessage
//{
//    public bool LeftButton { get; set; }
//    public bool RightButton { get; set; }
//    public bool UpButton { get; set; }
//    public bool DownButton { get; set; }
//    public bool AButton { get; set; }
//    public bool BButton { get; set; }
//    public int AnalogLeftRight { get; set; } // -100 to 100
//    public int AnalogUpDown { get; set; } // -100 to 100

//    public byte[] ToByteArray()
//    {
//        // Serialize the message into a byte array (implement serialization logic here)
//        return Encoding.UTF8.GetBytes($"{LeftButton},{RightButton},{UpButton},{DownButton},{AButton},{BButton},{AnalogLeftRight},{AnalogUpDown}");
//    }
//}