using GameControllerForZwift.Network.Dircon;

namespace GameControllerForZwift.Network.UnitTests;

public class BleUuidTests
{
    [Fact]
    public void FromShort_UsesBluetoothBase()
    {
        var uuid = BleUuid.FromShort(0x180A);
        Assert.Equal("0000180A-0000-1000-8000-00805F9B34FB", uuid.ToString());
        Assert.Equal(Convert.FromHexString("0000180A00001000800000805F9B34FB"), uuid.ToBytes());
    }

    [Fact]
    public void LongForm_BytesAreCanonicalBigEndian()
    {
        var uuid = BleUuid.Parse("00000003-19CA-4651-86E5-FA29DCDD09D1");
        Assert.Equal(Convert.FromHexString("0000000319ca465186e5fa29dcdd09d1"), uuid.ToBytes());
        Assert.Equal("00000003-19CA-4651-86E5-FA29DCDD09D1", uuid.ToString());
    }

    [Fact]
    public void FromBytes_RoundTripsAndEquals()
    {
        var bytes = Convert.FromHexString("a026ee0d0a7d4ab397faf1500f9feb8b");
        var uuid = BleUuid.FromBytes(bytes);
        Assert.Equal(bytes, uuid.ToBytes());
        Assert.Equal(BleUuid.Parse("A026EE0D-0A7D-4AB3-97FA-F1500F9FEB8B"), uuid);
        Assert.True(uuid == BleUuid.FromBytes(bytes));
        Assert.NotEqual(uuid, BleUuid.FromShort(0x1826));
    }

    [Fact]
    public void ShortAndLongForms_AreEqual()
    {
        Assert.Equal(BleUuid.FromShort(0x2A63), BleUuid.Parse("00002A63-0000-1000-8000-00805F9B34FB"));
        Assert.Equal(BleUuid.FromShort(0x2A63), BleUuid.Parse("0x2A63"));
        Assert.Equal(BleUuid.FromShort(0x2A63).GetHashCode(), BleUuid.Parse("2a63").GetHashCode());
    }
}
