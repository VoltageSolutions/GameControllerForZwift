using System.Text;
using GameControllerForZwift.Network.Dircon;

namespace GameControllerForZwift.Network.UnitTests;

/// <summary>Per-identity 0x3c device-info / RideOn / get-request replies (probe --identity). KICKR bytes must stay as captured.</summary>
public class ControllerIdentityTests
{
    private static readonly DeviceIdentity CaptureIdentity = new("KICKR BIKE SHIFT 0001", "100000001", "02-47-5A-00-00-01", "1.21.18", "3");

    // name "KICKR BIKE S" (12 bytes), serial "44100000001" NUL padded to 15 bytes, hardware revision "3".
    private const string Tail = "1a0c4b49434b522042494b452053" + "320f343431303030303030303100000000" + "3a0133";
    private const string FirmwareBytes = "120412001501";

    private static string Hex(byte[] b) => Convert.ToHexString(b).ToLowerInvariant();

    private static ZwiftControlProtocol Create(ControllerIdentity identity) => new(CaptureIdentity, null, identity);

    private static IReadOnlyList<(BleUuid CharUuid, byte[] Notification)> Write(ZwiftControlProtocol protocol, params byte[] value)
        => protocol.HandleWrite(DirconUuids.ZwiftSyncRx, value);

    private static string? SyncTxReply(ZwiftControlProtocol protocol, params byte[] value)
    {
        var replies = Write(protocol, value).Where(r => r.CharUuid == DirconUuids.ZwiftSyncTx).ToList();
        return replies.Count == 0 ? null : Hex(replies[0].Notification);
    }

    private static string ControllerInfo(string versionVarintHex) =>
        "3c080012330a31" + "08" + versionVarintHex + FirmwareBytes + Tail + "420408021000";

    [Fact]
    public void Kickr_DeviceInfo_UnchangedFromCapture()
    {
        const string captured =
            "3c080012390a370882041204120015011a0c4b49434b522042494b452053320f3434313030303030303031000000003a0133420408011024420408021000";
        Assert.Equal(captured, Hex(Create(ControllerIdentity.KickrBike).BuildDeviceInfoReply()));
        Assert.Equal(captured, Hex(new ZwiftControlProtocol(CaptureIdentity).BuildDeviceInfoReply()));
        Assert.Equal(captured, SyncTxReply(Create(ControllerIdentity.KickrBike), 0x00, 0x08, 0x00));
    }

    [Theory]
    [InlineData(ControllerIdentity.Ride, "8204")]    // protocol 0x0202: unknown for a real Ride, KICKR fallback
    [InlineData(ControllerIdentity.ClickV2, "8304")] // 0x0203 = 515
    [InlineData(ControllerIdentity.ClickV1, "8302")] // 0x0103 = 259
    [InlineData(ControllerIdentity.Play, "8402")]    // 0x0104 = 260
    public void Controller_DeviceInfo_IsUserInputOnly(ControllerIdentity identity, string versionVarintHex)
    {
        string expected = ControllerInfo(versionVarintHex);
        Assert.Equal(expected, Hex(Create(identity).BuildDeviceInfoReply()));
        Assert.Equal(expected, SyncTxReply(Create(identity), 0x00, 0x08, 0x00));
    }

    [Theory]
    [InlineData(ControllerIdentity.KickrBike, "526964654f6e0202")]
    [InlineData(ControllerIdentity.Ride, "526964654f6e0202")]
    [InlineData(ControllerIdentity.ClickV2, "526964654f6e0203")]
    [InlineData(ControllerIdentity.ClickV1, "526964654f6e0103")]
    [InlineData(ControllerIdentity.Play, "526964654f6e0104")]
    public void RideOnReply_PerIdentity(ControllerIdentity identity, string expected)
    {
        var protocol = Create(identity);
        var request = Encoding.ASCII.GetBytes("RideOn").Concat(new byte[] { 0x02, 0x04 }).ToArray();
        Assert.Equal(expected, SyncTxReply(protocol, request));
        Assert.True(protocol.RideOnCompleted);
    }

    [Theory]
    [InlineData(ControllerIdentity.KickrBike)]
    [InlineData(ControllerIdentity.Ride)]
    [InlineData(ControllerIdentity.ClickV2)]
    [InlineData(ControllerIdentity.ClickV1)]
    [InlineData(ControllerIdentity.Play)]
    public void GetRequests_CannedReplies_SameForAllIdentities(ControllerIdentity identity)
    {
        var protocol = Create(identity);
        Assert.Equal("3c088008121d08d80110031a0a080110ffff0318feff031a0a080210ffff0318feff03", SyncTxReply(protocol, 0x00, 0x08, 0x80, 0x08)); // 1024
        Assert.Equal("3c088306", SyncTxReply(protocol, 0x00, 0x08, 0x83, 0x06));                                                                  // 771
        Assert.Equal("3c089004120408012003", SyncTxReply(protocol, 0x00, 0x08, 0x90, 0x04));                                                      // 528
        Assert.Null(SyncTxReply(protocol, 0x00, 0x08, 0x10));                                                                                     // 16: no reply in the capture
    }

    [Theory]
    [InlineData(ControllerIdentity.KickrBike, "3c08800a12060a0405020607")]
    [InlineData(ControllerIdentity.Ride, null)]
    [InlineData(ControllerIdentity.ClickV2, null)]
    [InlineData(ControllerIdentity.ClickV1, null)]
    [InlineData(ControllerIdentity.Play, null)]
    public void SensorRelayPage_1280_OnlyForKickr(ControllerIdentity identity, string? expected)
        => Assert.Equal(expected, SyncTxReply(Create(identity), 0x00, 0x08, 0x80, 0x0A));

    [Theory]
    [InlineData(ControllerIdentity.KickrBike)]
    [InlineData(ControllerIdentity.Ride)]
    [InlineData(ControllerIdentity.ClickV2)]
    [InlineData(ControllerIdentity.ClickV1)]
    [InlineData(ControllerIdentity.Play)]
    public void SensorRelayConfig_0x44_IsAcknowledged(ControllerIdentity identity)
    {
        var protocol = Create(identity);
        Assert.Equal("3e08441000", SyncTxReply(protocol, 0x44, 0x20, 0x01));
        Assert.Equal("3e08441000", SyncTxReply(protocol, 0x44, 0x08, 0x01));
    }

    [Fact]
    public void DefaultProfileOptions_UseKickrIdentity()
    {
        Assert.Equal(ControllerIdentity.KickrBike, DeviceProfileOptions.Default.Controller);
        Assert.Equal(ControllerIdentity.KickrBike, DeviceProfileOptions.FromKind(DeviceProfileKind.Zwift).Controller);
    }
}
