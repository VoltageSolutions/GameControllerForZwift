using System.Net;
using GameControllerForZwift.Network.Dircon;
using GameControllerForZwift.Network.Mdns;

namespace GameControllerForZwift.Network.UnitTests;

/// <summary>Remaining branches of <see cref="MdnsAdvertiser"/> that need no network access.</summary>
public class MdnsAdvertiserBranchTests
{
    private static readonly IPAddress[] Addresses = { IPAddress.Parse("192.168.1.50") };

    [Fact]
    public void NotRunningBeforeStart_StopAndDisposeAreSafe()
    {
        var advertiser = new MdnsAdvertiser();
        Assert.False(advertiser.IsRunning);
        advertiser.Stop();
        advertiser.DisposeAsync().AsTask().GetAwaiter().GetResult();
        Assert.False(advertiser.IsRunning);
    }

    [Fact]
    public void Start_NullIdentity_Throws()
        => Assert.Throws<ArgumentNullException>(() => new MdnsAdvertiser().Start(null!, 1234));

    [Fact]
    public void BuildPrimaryProfile_NullArguments_Throw()
    {
        var advertiser = new MdnsAdvertiser();
        Assert.Throws<ArgumentNullException>(() => advertiser.BuildPrimaryProfile(null!, 1, Addresses));
        Assert.Throws<ArgumentNullException>(() => advertiser.BuildPrimaryProfile(DeviceIdentity.Default, 1, null!));
    }

    [Fact]
    public void BuildHostName_ReplacesSpacesWithDashes()
        => Assert.Equal("My-Bike-One.local", MdnsAdvertiser.BuildHostName("My Bike One").ToString());

    [Fact]
    public void InstanceNameWithDots_IsSanitisedToSingleLabel()
    {
        var identity = DeviceIdentity.Default with { Name = "Zwift v1.2 Bike" };
        var profile = new MdnsAdvertiser().BuildPrimaryProfile(identity, 1234, Addresses);
        Assert.DoesNotContain(".2", profile.HostName.ToString().Replace(".local", ""));
    }

    [Fact]
    public void ProfileOptions_AddObcPowerMfgDataAndExtraTxt()
    {
        var options = new DeviceProfileOptions(
            Advert: AdvertKind.Zwift, MfgData: "4A0906B653", TxtExtra: new[] { "foo=bar" }, AddObcUuid: true, AddPowerService: true);
        var profile = new MdnsAdvertiser(null, options).BuildPrimaryProfile(DeviceIdentity.Default, 1234, Addresses);
        var txt = profile.Resources.OfType<Makaretu.Dns.TXTRecord>().Single().Strings;
        var uuids = Assert.Single(txt, s => s.StartsWith("ble-service-uuids=", StringComparison.Ordinal));
        Assert.Contains(DeviceProfileOptions.ObcServiceUuid, uuids);
        Assert.EndsWith(",0x1818", uuids);
        Assert.Contains("manufacturer-data=4A0906B653", txt);
        Assert.Contains("foo=bar", txt);
        Assert.Contains("serial-number=" + DeviceIdentity.Default.SerialNumber, txt);
    }

    [Fact]
    public void KickrAdvert_WithPowerService_DoesNotDuplicate1818()
    {
        var options = new DeviceProfileOptions(AddPowerService: true);
        var profile = new MdnsAdvertiser(null, options).BuildPrimaryProfile(DeviceIdentity.Default, 1234, Addresses);
        var uuids = profile.Resources.OfType<Makaretu.Dns.TXTRecord>().Single().Strings.Single(s => s.StartsWith("ble-service-uuids=", StringComparison.Ordinal));
        Assert.Single(uuids.Split(','), p => p.Equals("0x1818", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetAdvertisedAddresses_ReturnsOnlyRoutableIPv4()
    {
        var addresses = MdnsAdvertiser.GetAdvertisedAddresses();
        Assert.All(addresses, a =>
        {
            Assert.Equal(System.Net.Sockets.AddressFamily.InterNetwork, a.AddressFamily);
            Assert.False(IPAddress.IsLoopback(a));
        });
        Assert.Equal(addresses.Count, addresses.Distinct().Count());
    }
}
