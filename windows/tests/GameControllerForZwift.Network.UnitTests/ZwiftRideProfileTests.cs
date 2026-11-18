using GameControllerForZwift.Network.ZwiftRide;

namespace GameControllerForZwift.Network.UnitTests;

public class ZwiftRideProfileTests
{
    [Fact]
    public void Advertisement_ListsKickrServicesPlusOpenBikeControl()
    {
        Assert.Equal(
            "0xFC82,0x1818,0x1826,00000001-19CA-4651-86E5-FA29DCDD09D1,A026EE0D-0A7D-4AB3-97FA-F1500F9FEB8B,d273f680-d548-419d-b9d1-fa0472345229",
            ZwiftRideProfile.AdvertisedBleServiceUuids);
        Assert.Equal("4A0906B653", ZwiftRideProfile.ManufacturerData);
    }

    [Fact]
    public void Services_AreCreatedFreshOnEveryCall()
    {
        var first = ZwiftRideProfile.CreateServices();
        var second = ZwiftRideProfile.CreateServices();
        Assert.NotSame(first, second);
        Assert.NotSame(first[0], second[0]);
        Assert.Equal(6, first.Count);
    }

    [Fact]
    public void FtmsFeature_IsServedWithTheCapturedValue()
    {
        var ftms = ZwiftRideProfile.CreateServices().Single(s => s.Uuid == ZwiftRideUuids.FitnessMachine);
        var feature = ftms.Characteristics.Single(c => c.Uuid == ZwiftRideUuids.FtmsFeature);
        Assert.Equal(Convert.FromHexString("8a4000001e200000"), feature.Value);
    }
}
