using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace GameControllerForZwift.Network.IntegrationTests.copilotv3
{
    public class DirconPacket
    {
        public byte MessageVersion { get; set; } = 1;
        public byte Identifier { get; set; }
        public byte SequenceNumber { get; set; }
        public byte ResponseCode { get; set; }
        public ushort Length { get; set; }
        public ushort Uuid { get; set; }
        public List<ushort> Uuids { get; set; } = new List<ushort>();
        public byte[] AdditionalData { get; set; } = Array.Empty<byte>();
        public bool IsRequest { get; set; }

        public byte[] Encode(int lastSequenceNumber)
        {
            var buffer = new List<byte>();
            SequenceNumber = (byte)(IsRequest ? lastSequenceNumber : 0);
            buffer.Add(MessageVersion);
            buffer.Add(Identifier);
            buffer.Add(SequenceNumber);
            buffer.Add(ResponseCode);
            buffer.Add((byte)(Length >> 8));
            buffer.Add((byte)(Length & 0xFF));
            buffer.AddRange(AdditionalData);
            return buffer.ToArray();
        }

        public static DirconPacket Decode(byte[] data)
        {
            if (data.Length < 6) throw new ArgumentException("Invalid packet length");

            var packet = new DirconPacket
            {
                MessageVersion = data[0],
                Identifier = data[1],
                SequenceNumber = data[2],
                ResponseCode = data[3],
                Length = (ushort)((data[4] << 8) | data[5]),
                AdditionalData = data.Length > 6 ? data[6..] : Array.Empty<byte>()
            };

            return packet;
        }
    }

    public class DirconProcessor
    {
        private TcpListener _server;
        private readonly Dictionary<TcpClient, NetworkStream> _clients = new();

        public DirconProcessor(int port)
        {
            _server = new TcpListener(System.Net.IPAddress.Any, port);
        }

        public void Start()
        {
            _server.Start();
            _server.BeginAcceptTcpClient(OnClientConnected, null);
            Console.WriteLine("DirconProcessor started.");
        }

        public void Stop()
        {
            foreach (var client in _clients.Keys)
            {
                client.Close();
            }
            _server.Stop();
        }

        private void OnClientConnected(IAsyncResult ar)
        {
            var client = _server.EndAcceptTcpClient(ar);
            var stream = client.GetStream();
            _clients[client] = stream;
            Console.WriteLine("Client connected.");

            var buffer = new byte[1024];
            stream.BeginRead(buffer, 0, buffer.Length, OnDataReceived, (client, buffer));

            _server.BeginAcceptTcpClient(OnClientConnected, null);
        }

        private void OnDataReceived(IAsyncResult ar)
        {
            var (client, buffer) = ((TcpClient, byte[]))ar.AsyncState;
            var stream = _clients[client];

            try
            {
                var bytesRead = stream.EndRead(ar);
                if (bytesRead > 0)
                {
                    var packet = DirconPacket.Decode(buffer[..bytesRead]);
                    Console.WriteLine($"Received packet: {packet.Identifier}");

                    // Process the packet and send a response if needed
                    var response = new DirconPacket
                    {
                        Identifier = packet.Identifier,
                        ResponseCode = 0x00, // Success
                        AdditionalData = Encoding.UTF8.GetBytes("Response")
                    };

                    var responseData = response.Encode(packet.SequenceNumber);
                    stream.Write(responseData, 0, responseData.Length);
                }

                stream.BeginRead(buffer, 0, buffer.Length, OnDataReceived, (client, buffer));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                _clients.Remove(client);
                client.Close();
            }
        }
    }
}
