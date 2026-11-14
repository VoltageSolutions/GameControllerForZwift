using System.Net;
using GameControllerForZwift.Network.ZwiftRide;
using GameControllerForZwift.Network.Mdns;

namespace GameControllerForZwift.Network.UnitTests;

/// <summary>Remaining branches of <see cref="MdnsAdvertiser"/> that need no network access.</summary>
public class MdnsAdvertiserBranchTests
{
    private static readonly IPAddress[] Addresses = { IPAddress.Parse("192.168.1.50") };

    [Fact]
    public async Task NotRunningBeforeStart_StopAndDisposeAreSafe()
    {
        var advertiser = new MdnsAdvertiser();
        Assert.False(advertiser.IsRunning);
        advertiser.Stop();
        await advertiser.DisposeAsync();
        Assert.False(advertiser.IsRunning);
    }

    [Fact]
    public void Start_NullIdentity_Throws()
        => Assert.Throws<ArgumentNullException>(() => new MdnsAdvertiser().Start(null!, 1234));

    [Fact]
    public void BuildServiceProfile_NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => MdnsAdvertiser.BuildServiceProfile(null!, 1, Addresses));
        Assert.Throws<ArgumentNullException>(() => MdnsAdvertiser.BuildServiceProfile(DeviceIdentity.Default, 1, null!));
    }

    [Fact]
    public void BuildHostName_ReplacesSpacesWithDashes()
        => Assert.Equal("My-Bike-One.local", MdnsAdvertiser.BuildHostName("My Bike One").ToString());

    [Fact]
    public void InstanceNameWithDots_IsSanitisedToSingleLabel()
    {
        var identity = DeviceIdentity.Default with { Name = "Zwift v1.2 Bike" };
        var profile = MdnsAdvertiser.BuildServiceProfile(identity, 1234, Addresses);
        Assert.DoesNotContain(".2", profile.HostName.ToString().Replace(".local", ""));
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
