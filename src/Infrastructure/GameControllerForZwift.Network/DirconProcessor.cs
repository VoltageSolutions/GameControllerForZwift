using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Makaretu.Dns;

namespace GameControllerForZwift.Network
{
    public class DirconProcessor : IDisposable
    {
        private List<DirconProcessorService> services;
        private string mac;
        private ushort serverPort;
        private string serialN;
        private string serverName;
        private TcpListener server;
        private MulticastService mdnsServer;
        private ServiceProfile mdnsService;

        public DirconProcessor(List<DirconProcessorService> myServices, string servName, ushort servPort, string servSn, string myMac)
        {
            services = myServices;
            mac = myMac;
            serverPort = servPort;
            serialN = servSn;
            serverName = servName;

            Debug.WriteLine($"In the constructor of dircon processor for {serverName}");
            foreach (var myService in myServices)
            {
                myService.SetParent(this);
            }
        }

        public void Dispose()
        {
            server?.Stop();
            mdnsServer?.Dispose();
        }

        public bool InitServer()
        {
            Debug.WriteLine($"Initializing dircon tcp server for {serverName}");
            if (server == null)
            {
                server = new TcpListener(IPAddress.Any, serverPort);
                server.Start();
                Debug.WriteLine($"Dircon TCP Server built {serverName}");
                Task.Run(() => AcceptClientsAsync());
            }
            return true;
        }

        private async Task AcceptClientsAsync()
        {
            while (true)
            {
                var client = await server.AcceptTcpClientAsync();
                Debug.WriteLine($"New client connected to {serverName}");
                // Handle the client connection
            }
        }

        public void InitAdvertising()
        {
            if (mdnsServer == null)
            {
                Debug.WriteLine($"Dircon Adv init for {serverName}");
                mdnsServer = new MulticastService();
                //mdnsService = new ServiceProfile($"_wahoo-fitness-tnp._tcp.local.", serverPort)
                //{
                //    Name = serverName,
                //    Attributes = new Dictionary<string, string>
                //    {
                //        { "mac-address", mac },
                //        { "serial-number", serialN }
                //    }
                //};

                //mdnsServer.Advertise(mdnsService);
                mdnsServer.Start();
            }
        }
    }

    public class DirconProcessorService
    {
        public void SetParent(DirconProcessor parent)
        {
            // Implement the logic to set the parent
        }
    }
}