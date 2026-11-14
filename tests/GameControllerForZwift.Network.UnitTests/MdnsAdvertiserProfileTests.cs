using System.Net;
using GameControllerForZwift.Network.ZwiftRide;
using GameControllerForZwift.Network.ZwiftRide;
using GameControllerForZwift.Network.Mdns;
using Makaretu.Dns;

namespace GameControllerForZwift.Network.UnitTests;

public class MdnsAdvertiserProfileTests
{
    private static readonly IPAddress[] Addresses = { IPAddress.Parse("192.168.1.50"), IPAddress.Parse("10.0.0.7") };

    private static ServiceProfile Build(int port = 36866)
        => MdnsAdvertiser.BuildServiceProfile(DeviceIdentity.Default, port, Addresses);

    [Fact]
    public void Profile_HasWahooServiceTypeAndInstanceName()
    {
        var profile = Build();
        Assert.Equal("_wahoo-fitness-tnp._tcp", MdnsAdvertiser.ServiceType);
        Assert.StartsWith("_wahoo-fitness-tnp._tcp", profile.ServiceName.ToString());
        Assert.Equal("Game Controller for Zwift", profile.InstanceName.ToString().Replace("\\032", " ").Split('.')[0]);
    }

    [Fact]
    public void Profile_SrvCarriesPortAndHostTarget()
    {
        var srv = Assert.Single(Build(40000).Resources.OfType<SRVRecord>());
        Assert.Equal(40000, srv.Port);
        Assert.Equal("Game-Controller-for-Zwift.local", srv.Target.ToString());
    }

    [Fact]
    public void Profile_TxtIsTheZwiftRideAdvertisement()
    {
        var txt = Assert.Single(Build().Resources.OfType<TXTRecord>());
        var id = DeviceIdentity.Default;
        Assert.Equal(new[]
        {
            "ble-service-uuids=0xFC82,0x1818,0x1826,00000001-19CA-4651-86E5-FA29DCDD09D1,A026EE0D-0A7D-4AB3-97FA-F1500F9FEB8B,d273f680-d548-419d-b9d1-fa0472345229",
            "mac-address=" + id.MacAddress,
            "serial-number=" + id.SerialNumber,
            "manufacturer-data=4A0906B653",
        }, txt.Strings.ToArray());
    }

    [Fact]
    public void Profile_AddressRecordsMatchGivenAddressesOnly()
    {
        var a = Build().Resources.OfType<ARecord>().Select(r => r.Address).ToList();
        Assert.Equal(Addresses.Length, a.Count);
        Assert.All(Addresses, ip => Assert.Contains(ip, a));
    }

    [Fact]
    public void GoodbyeRecords_CoverEveryRecordWithZeroTtl()
    {
        var profile = Build();
        var count = profile.Resources.Count;
        var goodbye = MdnsAdvertiser.BuildGoodbyeRecords(new[] { profile });
        Assert.Equal(count, goodbye.Count);
        Assert.All(goodbye, r => Assert.Equal(TimeSpan.Zero, r.TTL));
        Assert.Contains(goodbye, r => r is SRVRecord);
        Assert.Contains(goodbye, r => r is TXTRecord);
        Assert.Contains(goodbye, r => r is ARecord);
    }
}
