using System.Net;
using GameControllerForZwift.Network.Dircon;
using GameControllerForZwift.Network.Mdns;
using Makaretu.Dns;

namespace GameControllerForZwift.Network.UnitTests;

public class MdnsAdvertiserProfileTests
{
    private static readonly IPAddress[] Addresses = { IPAddress.Parse("192.168.1.50"), IPAddress.Parse("10.0.0.7") };

    private static ServiceProfile Build(int port = 36866)
        => new MdnsAdvertiser().BuildPrimaryProfile(DeviceIdentity.Default, port, Addresses);

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
    public void Profile_TxtHasIdentityEntries()
    {
        var txt = Assert.Single(Build().Resources.OfType<TXTRecord>());
        var id = DeviceIdentity.Default;
        Assert.Contains(txt.Strings, s => s.StartsWith("ble-service-uuids=", StringComparison.Ordinal) && s.Contains("0xFC82", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("mac-address=" + id.MacAddress, txt.Strings);
        Assert.Contains("serial-number=" + id.SerialNumber, txt.Strings);
    }

    [Fact]
    public void Profile_TxtIncludesManufacturerDataWhenConfigured()
    {
        var options = DeviceProfileOptions.Default with { MfgData = "abcd" };
        var profile = new MdnsAdvertiser(null, options).BuildPrimaryProfile(DeviceIdentity.Default, 1234, Addresses);
        var txt = Assert.Single(profile.Resources.OfType<TXTRecord>());
        Assert.Contains("manufacturer-data=abcd", txt.Strings);
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
