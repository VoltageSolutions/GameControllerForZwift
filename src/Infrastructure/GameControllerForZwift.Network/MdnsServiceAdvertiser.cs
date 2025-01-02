//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net;
//using System.Net.NetworkInformation;
//using Tmds.MDns;

//namespace GameControllerForZwift.Network
//{
//    public class MdnsServiceAdvertiser
//    {
//        private readonly ServiceBrowser _serviceBrowser;
//        private readonly Responder _responder;
//        private readonly string _serviceName;
//        private readonly int _port;
//        private readonly Dictionary<string, string> _txtRecords;

//        public MdnsServiceAdvertiser(string serviceName, int port, Dictionary<string, string> txtRecords)
//        {
//            _serviceName = serviceName;
//            _port = port;
//            _txtRecords = txtRecords;
//            _serviceBrowser = new ServiceBrowser();
//            _responder = new Responder();
//        }

//        /// <summary>
//        /// Starts advertising the service on the specified network interface.
//        /// </summary>
//        /// <param name="networkInterface">The network interface to bind to (optional).</param>
//        public void Start(NetworkInterface networkInterface = null)
//        {
//            if (networkInterface != null)
//            {
//                var ipProperties = networkInterface.GetIPProperties();
//                var ipv4Address = ipProperties.UnicastAddresses
//                    .FirstOrDefault(ua => ua.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?.Address;

//                if (ipv4Address == null)
//                {
//                    throw new InvalidOperationException("The selected network interface does not have an IPv4 address.");
//                }

//                _responder.NetworkInterface = networkInterface;
//            }

//            var serviceInstance = new ServiceInstance(
//                $"{_serviceName}._wahoo-fitness-tnp.local",
//                "local",
//                _port,
//                GetTxtRecords()
//            );

//            _responder.Advertise(serviceInstance);
//            Console.WriteLine($"Started advertising {_serviceName} on port {_port}");
//        }

//        /// <summary>
//        /// Stops the service advertisement.
//        /// </summary>
//        public void Stop()
//        {
//            _responder.Stop();
//            Console.WriteLine("Stopped advertising service.");
//        }

//        /// <summary>
//        /// Returns a list of all available network interfaces.
//        /// </summary>
//        public static IEnumerable<NetworkInterface> GetAllNetworkInterfaces()
//        {
//            return NetworkInterface.GetAllNetworkInterfaces()
//                .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
//                             ni.NetworkInterfaceType != NetworkInterfaceType.Loopback);
//        }

//        /// <summary>
//        /// Prints a list of all available network interfaces for selection.
//        /// </summary>
//        public static void PrintNetworkInterfaces()
//        {
//            var interfaces = GetAllNetworkInterfaces();
//            Console.WriteLine("Available Network Interfaces:");
//            int index = 0;
//            foreach (var ni in interfaces)
//            {
//                Console.WriteLine($"{index}: {ni.Name} ({ni.Description})");
//                index++;
//            }
//        }

//        private TxtRecord GetTxtRecords()
//        {
//            var txtRecord = new TxtRecord();
//            foreach (var kvp in _txtRecords)
//            {
//                txtRecord.Add(kvp.Key, kvp.Value);
//            }
//            return txtRecord;
//        }
//    }

//}