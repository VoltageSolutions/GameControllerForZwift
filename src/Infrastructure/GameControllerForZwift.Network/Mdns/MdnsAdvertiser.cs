using GameControllerForZwift.Network.Dircon;
using Makaretu.Dns;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace GameControllerForZwift.Network.Mdns
{
    /// <summary>An additional mDNS service instance advertised next to (or instead of) the DirCon one.</summary>
    public sealed record MdnsExtraService(string InstanceName, string ServiceType, int Port, IReadOnlyList<string> TxtStrings);

    /// <summary>
    /// Advertises the emulated Wahoo DirCon device (_wahoo-fitness-tnp._tcp) on the local network so Zwift can discover it.
    /// Mirrors what a real KICKR BIKE announces (see docs/captures/kickr-bike-capture-analysis.md): PTR + SRV + TXT, with A records
    /// for THIS machine's own IPv4 addresses.
    /// </summary>
    public sealed class MdnsAdvertiser : IAsyncDisposable
    {
        public const string ServiceType = "_wahoo-fitness-tnp._tcp";
        public const string BleServiceUuids = "0xFC82,0x1818,0x1826,00000001-19CA-4651-86E5-FA29DCDD09D1,A026EE0D-0A7D-4AB3-97FA-F1500F9FEB8B";

        private static readonly string[] VirtualAdapterMarkers =
        {
            "virtual", "vmware", "vbox", "hyper-v", "vethernet", "wsl", "docker", "tap-", "tap ", "npcap", "loopback",
            "bluetooth", "tailscale", "zerotier", "hamachi", "vpn", "pseudo", "miniport"
        };

        private readonly ILogger<MdnsAdvertiser>? _logger;
        private readonly object _gate = new();
        private MulticastService? _mdns;
        private ServiceDiscovery? _discovery;
        private List<ServiceProfile> _profiles = new();

        private readonly AdvertKind _advert;
        private readonly DeviceProfileOptions _profileOptions;

        public MdnsAdvertiser(ILogger<MdnsAdvertiser>? logger = null, DeviceProfileOptions? profile = null)
        {
            _logger = logger;
            _profileOptions = profile ?? DeviceProfileOptions.Default;
            _advert = _profileOptions.Advert;
        }

        public bool IsRunning
        {
            get { lock (_gate) return _mdns != null; }
        }

        /// <summary>Starts advertising. <paramref name="port"/> must be the port the DirCon server actually bound.</summary>
        public void Start(DeviceIdentity identity, int port)
            => Start(identity, port, null, true);

        /// <summary>
        /// Starts advertising, optionally with an additional service instance (e.g. _zwift-protocol._tcp) that reuses the same host A records.
        /// With <paramref name="advertisePrimary"/> false only <paramref name="extra"/> is advertised.
        /// </summary>
        public void Start(DeviceIdentity identity, int port, MdnsExtraService? extra, bool advertisePrimary)
        {
            ArgumentNullException.ThrowIfNull(identity);
            lock (_gate)
            {
                if (_mdns != null)
                    return;
                if (!advertisePrimary && extra == null)
                    return;

                var addresses = GetAdvertisedAddresses();
                if (addresses.Count == 0)
                    _logger?.LogWarning("mDNS: no usable IPv4 address found; Zwift will not be able to resolve this device.");

                // Instance name may contain spaces (like the real bike) but must be a single DNS label (no dots).
                var instanceName = identity.Name.Replace('.', ' ');
                var hostName = new DomainName(instanceName.Replace(' ', '-') + ".local");

                // Service type without ".local": ServiceProfile appends the domain itself.
                var txt = new List<string>
                {
                    "ble-service-uuids=" + DeviceProfiles.BleServiceUuids(_advert) + (_profileOptions.AddObcUuid ? "," + DeviceProfileOptions.ObcServiceUuid : "") + (_profileOptions.AddPowerService && !DeviceProfiles.BleServiceUuids(_advert).Contains("0x1818", StringComparison.OrdinalIgnoreCase) ? ",0x1818" : ""),
                    "mac-address=" + identity.MacAddress,
                    "serial-number=" + identity.SerialNumber,
                };
                if (!string.IsNullOrEmpty(_profileOptions.MfgData))
                    txt.Add("manufacturer-data=" + _profileOptions.MfgData);
                if (_profileOptions.TxtExtra != null)
                    txt.AddRange(_profileOptions.TxtExtra);
                var profile = BuildProfile(instanceName, ServiceType, port, addresses, hostName, txt);
                ServiceProfile? extraProfile = null;
                if (extra != null)
                    extraProfile = BuildProfile(extra.InstanceName.Replace('.', ' '), extra.ServiceType, extra.Port, addresses, hostName, extra.TxtStrings);

                var mdns = new MulticastService();
                mdns.QueryReceived += (_, e) =>
                {
                    if (_logger is null || !_logger.IsEnabled(LogLevel.Debug)) return;
                    try
                    {
                        var questions = string.Join(", ", e.Message.Questions.Select(q => $"{q.Name} {q.Type}"));
                        _logger.LogDebug("mDNS query from {Remote}: {Questions}", e.RemoteEndPoint, questions);
                    }
                    catch (Exception ex) { _logger.LogDebug(ex, "mDNS query logging failed."); }
                };
                var discovery = new ServiceDiscovery(mdns);
                mdns.Start();

                var toAdvertise = new List<ServiceProfile>();
                if (advertisePrimary) toAdvertise.Add(profile);
                if (extraProfile != null) toAdvertise.Add(extraProfile);

                try
                {
                    if (advertisePrimary && discovery.Probe(profile))
                        _logger?.LogWarning("mDNS: an instance named '{Name}' already exists on the network; advertising anyway.", instanceName);
                }
                catch (Exception ex)
                {
                    _logger?.LogDebug(ex, "mDNS probe failed; continuing.");
                }

                foreach (var p in toAdvertise)
                {
                    discovery.Advertise(p);
                    discovery.Announce(p);
                }

                _mdns = mdns;
                _discovery = discovery;
                _profiles = toAdvertise;
                _logger?.LogInformation("mDNS: advertising {Services} on port {Port} at {Addresses}",
                    string.Join(", ", toAdvertise.Select(p => $"'{p.InstanceName}' ({p.ServiceName}:{p.Resources.OfType<SRVRecord>().FirstOrDefault()?.Port})")),
                    port, string.Join(", ", addresses));
            }
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
                    if (_mdns != null && _profiles.Count > 0)
                    {
                        var goodbye = new Message { QR = true };
                        foreach (var p in _profiles)
                            foreach (var rr in p.Resources)
                            {
                                rr.TTL = TimeSpan.Zero;
                                goodbye.Answers.Add(rr);
                            }
                        _mdns.SendAnswer(goodbye, false);
                        _logger?.LogInformation("mDNS: goodbye (TTL 0) sent for {Count} service(s).", _profiles.Count);
                    }
                }
                catch (Exception ex) { _logger?.LogDebug(ex, "mDNS goodbye failed."); }
                try { foreach (var p in _profiles) _discovery?.Unadvertise(p); } catch (Exception ex) { _logger?.LogDebug(ex, "mDNS unadvertise failed."); }
                try { _discovery?.Dispose(); } catch (Exception ex) { _logger?.LogDebug(ex, "mDNS discovery dispose failed."); }
                try { _mdns?.Stop(); } catch (Exception ex) { _logger?.LogDebug(ex, "mDNS stop failed."); }
                try { _mdns?.Dispose(); } catch (Exception ex) { _logger?.LogDebug(ex, "mDNS dispose failed."); }
                _discovery = null;
                _mdns = null;
                _profiles = new();
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
    }
}
