using GameControllerForZwift.Network.ZwiftRide;
using Makaretu.Dns;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace GameControllerForZwift.Network.Mdns
{
    /// <summary>
    /// Advertises the emulated Zwift Ride device over Wahoo Direct Connect (DIRCON, _wahoo-fitness-tnp._tcp) on the local network so Zwift can discover it.
    /// Mirrors what a real KICKR BIKE announces (see docs/captures/kickr-bike-capture-analysis.md): PTR + SRV + TXT, with A records
    /// for THIS machine's own IPv4 addresses.
    /// </summary>
    public sealed partial class MdnsAdvertiser : IAsyncDisposable
    {
        public const string ServiceType = "_wahoo-fitness-tnp._tcp";

        private static readonly string[] VirtualAdapterMarkers =
        {
            "virtual", "vmware", "vbox", "hyper-v", "vethernet", "wsl", "docker", "tap-", "tap ", "npcap", "loopback",
            "bluetooth", "tailscale", "zerotier", "hamachi", "vpn", "pseudo", "miniport"
        };

        private readonly ILogger<MdnsAdvertiser> _logger;
        private readonly object _gate = new();
        private MulticastService? _mdns;
        private ServiceDiscovery? _discovery;
        private ServiceProfile? _serviceProfile;

        public MdnsAdvertiser(ILogger<MdnsAdvertiser>? logger = null)
        {
            _logger = logger ?? NullLogger<MdnsAdvertiser>.Instance;
        }

        public bool IsRunning
        {
            get { lock (_gate) return _mdns != null; }
        }

        /// <summary>Starts advertising. <paramref name="port"/> must be the port the Direct Connect server actually bound.</summary>
        public void Start(DeviceIdentity identity, int port)
        {
            ArgumentNullException.ThrowIfNull(identity);
            lock (_gate)
            {
                if (_mdns != null)
                    return;

                var addresses = GetAdvertisedAddresses();
                if (addresses.Count == 0)
                    LogNoIpv4Address();

                var instanceName = identity.Name.Replace('.', ' ');
                var profile = BuildServiceProfile(identity, port, addresses);

                var mdns = new MulticastService();
                mdns.QueryReceived += (_, e) =>
                {
                    if (!_logger.IsEnabled(LogLevel.Debug)) return;
                    try
                    {
                        var questions = string.Join(", ", e.Message.Questions.Select(q => $"{q.Name} {q.Type}"));
                        LogQuery(e.RemoteEndPoint, questions);
                    }
                    catch (Exception ex) { LogQueryLoggingFailed(ex); }
                };
                var discovery = new ServiceDiscovery(mdns);
                mdns.Start();

                try
                {
                    if (discovery.Probe(profile))
                        LogInstanceExists(instanceName);
                }
                catch (Exception ex)
                {
                    LogProbeFailed(ex);
                }

                discovery.Advertise(profile);
                discovery.Announce(profile);

                _mdns = mdns;
                _discovery = discovery;
                _serviceProfile = profile;
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    var addressList = string.Join(", ", addresses);
                    LogAdvertising(profile.InstanceName, profile.ServiceName, port, addressList);
                }
            }
        }

        /// <summary>The DNS host name (single label plus ".local") derived from the advertised instance name.</summary>
        internal static DomainName BuildHostName(string instanceName)
            => new(instanceName.Replace(' ', '-') + ".local");

        /// <summary>Builds the _wahoo-fitness-tnp._tcp profile (PTR, SRV, TXT, A) without touching the network.</summary>
        internal static ServiceProfile BuildServiceProfile(DeviceIdentity identity, int port, IReadOnlyList<IPAddress> addresses)
        {
            ArgumentNullException.ThrowIfNull(identity);
            ArgumentNullException.ThrowIfNull(addresses);
            // Instance name may contain spaces (like the real bike) but must be a single DNS label (no dots).
            var instanceName = identity.Name.Replace('.', ' ');
            var hostName = BuildHostName(instanceName);

            var txt = new List<string>
            {
                "ble-service-uuids=" + ZwiftRideProfile.AdvertisedBleServiceUuids,
                "mac-address=" + identity.MacAddress,
                "serial-number=" + identity.SerialNumber,
                "manufacturer-data=" + ZwiftRideProfile.ManufacturerData,
            };
            return BuildProfile(instanceName, ServiceType, port, addresses, hostName, txt);
        }

        /// <summary>Every record of the given profiles re-stamped with TTL 0 (mDNS goodbye). Mutates the profiles' records.</summary>
        internal static IReadOnlyList<ResourceRecord> BuildGoodbyeRecords(IEnumerable<ServiceProfile> profiles)
        {
            var records = new List<ResourceRecord>();
            foreach (var p in profiles)
                foreach (var rr in p.Resources)
                {
                    rr.TTL = TimeSpan.Zero;
                    records.Add(rr);
                }
            return records;
        }

        private static ServiceProfile BuildProfile(string instanceName, string serviceType, int port, IReadOnlyList<IPAddress> addresses, DomainName hostName, IReadOnlyList<string> txtStrings)
        {
            var profile = new ServiceProfile(instanceName, serviceType, (ushort)port, addresses);
            profile.HostName = hostName;

            var resources = new List<ResourceRecord>();
            foreach (var rr in profile.Resources)
            {
                if (rr is SRVRecord srv)
                {
                    srv.Target = hostName;
                    srv.Port = (ushort)port;
                    resources.Add(srv);
                }
                else if (rr is AddressRecord || rr is TXTRecord)
                {
                    // Replaced below: our own addresses, our own TXT.
                }
                else
                {
                    resources.Add(rr);
                }
            }

            resources.Add(new TXTRecord
            {
                Name = profile.FullyQualifiedName,
                Strings = txtStrings.ToList(),
                TTL = TimeSpan.FromSeconds(4500),
            });

            foreach (var address in addresses)
            {
                resources.Add(new ARecord
                {
                    Name = hostName,
                    Address = address,
                    TTL = TimeSpan.FromSeconds(120),
                });
            }

            profile.Resources = resources;
            return profile;
        }

        public void Stop()
        {
            lock (_gate)
            {
                // Explicit goodbye first: every record of our profiles (PTR, SRV, TXT, A) re-sent with TTL 0 so Zwift drops the device at once.
                try
                {
                    if (_mdns != null && _serviceProfile != null)
                    {
                        var goodbye = new Message { QR = true };
                        foreach (var rr in BuildGoodbyeRecords(new[] { _serviceProfile }))
                            goodbye.Answers.Add(rr);
                        _mdns.SendAnswer(goodbye, false);
                        LogGoodbyeSent();
                    }
                }
                catch (Exception ex) { LogGoodbyeFailed(ex); }
                try { if (_serviceProfile != null) _discovery?.Unadvertise(_serviceProfile); } catch (Exception ex) { LogUnadvertiseFailed(ex); }
                try { _discovery?.Dispose(); } catch (Exception ex) { LogDiscoveryDisposeFailed(ex); }
                try { _mdns?.Stop(); } catch (Exception ex) { LogStopFailed(ex); }
                try { _mdns?.Dispose(); } catch (Exception ex) { LogDisposeFailed(ex); }
                _discovery = null;
                _mdns = null;
                _serviceProfile = null;
            }
        }

        public ValueTask DisposeAsync()
        {
            Stop();
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// This machine's IPv4 addresses to advertise: up, non-loopback, non-tunnel, non-link-local interfaces, skipping
        /// obviously virtual adapters; interfaces with an IPv4 default gateway are preferred.
        /// </summary>
        public static IReadOnlyList<IPAddress> GetAdvertisedAddresses()
        {
            var candidates = new List<(IPAddress Address, bool HasGateway, bool Virtual)>();
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                try
                {
                    if (nic.OperationalStatus != OperationalStatus.Up) continue;
                    if (nic.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel) continue;

                    var props = nic.GetIPProperties();
                    var hasGateway = props.GatewayAddresses.Any(g =>
                        g.Address.AddressFamily == AddressFamily.InterNetwork && !g.Address.Equals(IPAddress.Any));
                    var text = (nic.Name + " " + nic.Description).ToLowerInvariant();
                    var isVirtual = VirtualAdapterMarkers.Any(m => text.Contains(m));

                    foreach (var ua in props.UnicastAddresses)
                    {
                        var ip = ua.Address;
                        if (ip.AddressFamily != AddressFamily.InterNetwork) continue;
                        if (IPAddress.IsLoopback(ip) || ip.Equals(IPAddress.Any)) continue;
                        var b = ip.GetAddressBytes();
                        if (b[0] == 169 && b[1] == 254) continue; // link-local
                        candidates.Add((ip, hasGateway, isVirtual));
                    }
                }
                catch (NetworkInformationException)
                {
                    // Interface vanished or is unreadable; skip it.
                }
            }

            IEnumerable<(IPAddress Address, bool HasGateway, bool Virtual)> pick = candidates.Where(c => c.HasGateway && !c.Virtual);
            if (!pick.Any()) pick = candidates.Where(c => !c.Virtual);
            if (!pick.Any()) pick = candidates.Where(c => c.HasGateway);
            if (!pick.Any()) pick = candidates;

            return pick.Select(c => c.Address).Distinct().ToList();
        }

        #region Logging

        [LoggerMessage(Level = LogLevel.Warning, Message = "mDNS: no usable IPv4 address found; Zwift will not be able to resolve this device.")]
        private partial void LogNoIpv4Address();

        [LoggerMessage(Level = LogLevel.Debug, Message = "mDNS query from {Remote}: {Questions}")]
        private partial void LogQuery(IPEndPoint remote, string questions);

        [LoggerMessage(Level = LogLevel.Debug, Message = "mDNS query logging failed.")]
        private partial void LogQueryLoggingFailed(Exception exception);

        [LoggerMessage(Level = LogLevel.Warning, Message = "mDNS: an instance named '{Name}' already exists on the network; advertising anyway.")]
        private partial void LogInstanceExists(string name);

        [LoggerMessage(Level = LogLevel.Debug, Message = "mDNS probe failed; continuing.")]
        private partial void LogProbeFailed(Exception exception);

        [LoggerMessage(Level = LogLevel.Information, Message = "mDNS: advertising '{Name}' ({Service}:{Port}) at {Addresses}")]
        private partial void LogAdvertising(DomainName name, DomainName service, int port, string addresses);

        [LoggerMessage(Level = LogLevel.Information, Message = "mDNS: goodbye (TTL 0) sent.")]
        private partial void LogGoodbyeSent();

        [LoggerMessage(Level = LogLevel.Debug, Message = "mDNS goodbye failed.")]
        private partial void LogGoodbyeFailed(Exception exception);

        [LoggerMessage(Level = LogLevel.Debug, Message = "mDNS unadvertise failed.")]
        private partial void LogUnadvertiseFailed(Exception exception);

        [LoggerMessage(Level = LogLevel.Debug, Message = "mDNS discovery dispose failed.")]
        private partial void LogDiscoveryDisposeFailed(Exception exception);

        [LoggerMessage(Level = LogLevel.Debug, Message = "mDNS stop failed.")]
        private partial void LogStopFailed(Exception exception);

        [LoggerMessage(Level = LogLevel.Debug, Message = "mDNS dispose failed.")]
        private partial void LogDisposeFailed(Exception exception);

        #endregion
    }
}
