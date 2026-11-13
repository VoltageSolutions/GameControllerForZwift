using GameControllerForZwift.Network.Dircon;

namespace GameControllerForZwift.Network.UnitTests;

/// <summary>Frame builders compared with byte strings from docs/captures/kickr-bike-capture-analysis.md.</summary>
public class RideFrameTests
{
    private static string Hex(byte[] b) => Convert.ToHexString(b);

    [Fact]
    public void ChainringFrame_MatchesCapture() =>
        Assert.Equal("4232020801", Hex(ZwiftControlProtocol.BuildChainringFrame(1)));

    [Theory]
    [InlineData(1, "4232022001")]
    [InlineData(3, "4232022003")]
    [InlineData(4, "4232022004")]
    [InlineData(5, "4232022005")]
    public void RearCogFrame_MatchesCapture(int gear, string expected) =>
        Assert.Equal(expected, Hex(ZwiftControlProtocol.BuildRearCogFrame(gear)));

    [Fact]
    public void RideData_FullFields_MatchCapture()
    {
        Assert.Equal("030823104818E60220002888503000", Hex(ZwiftControlProtocol.BuildRideData(35, 72, 358, 10248)));
        Assert.Equal("03080A10051874200028941A3000", Hex(ZwiftControlProtocol.BuildRideData(10, 5, 116, 3348)));
        Assert.Equal("03080010001800200028003000", Hex(ZwiftControlProtocol.BuildRideData(0, 0, 0, 0)));
    }

    [Fact]
    public void PowerMeasurement_MatchesCapture()
    {
        Assert.Equal("3C002300938AFD6600004E784F734542",
            Hex(ZwiftControlProtocol.BuildPowerMeasurement(35, 0x8A93, 0x66FD, 0x784E, 0x734F, 0x4245)));
        Assert.Equal("3C000000E67EEB66000081494373EEED",
            Hex(ZwiftControlProtocol.BuildPowerMeasurement(0, 0x7EE6, 0x66EB, 0x4981, 0x7343, 0xEDEE)));
    }

    [Fact]
    public void IndoorBikeData_MatchesCapture()
    {
        // 16.63 km/h, 71 rpm (142 half-rpm units), 39 W
        Assert.Equal("44007F068E002700", Hex(ZwiftControlProtocol.BuildIndoorBikeData(1663, 71, 39)));
        Assert.Equal("4400000000000000", Hex(ZwiftControlProtocol.BuildIndoorBikeData(0, 0, 0)));
    }

    [Fact]
    public void ClickKeyPad_Frames()
    {
        Assert.Equal("3708011001", Hex(ZwiftControlProtocol.BuildClickKeyPadFrame(false, false)));
        Assert.Equal("3708001001", Hex(ZwiftControlProtocol.BuildClickKeyPadFrame(true, false)));
        Assert.Equal("3708011000", Hex(ZwiftControlProtocol.BuildClickKeyPadFrame(false, true)));
    }

    [Fact]
    public void PlayKeyPad_Frames()
    {
        Assert.Equal("07080010011801200128013001380140C8014800", Hex(ZwiftControlProtocol.BuildPlayKeyPadFrame(true, true)));
        Assert.Equal("070801100118012001280130013801400048" + "00", Hex(ZwiftControlProtocol.BuildPlayKeyPadFrame(false, false)));
    }

    [Fact]
    public void DescribeProtobuf_DecodesCapturedThreeF()
    {
        var payload = Convert.FromHexString("08011000220C40EC2750E80558813E609003");
        Assert.Equal("1:v=1 2:v=0 4:{8:v=5100 10:v=744 11:v=7937 12:v=400}", ZwiftControlProtocol.DescribeProtobuf(payload));
        Assert.Equal("1:v=0 2:v=2 4:{5:v=256}", ZwiftControlProtocol.DescribeProtobuf(Convert.FromHexString("0800100222032880 02".Replace(" ", ""))));
    }

    [Fact]
    public void RideSimulator_CrankCounters_FollowCadence()
    {
        var sim = new RideSimulator();
        RideSample s = default;
        for (int i = 0; i < 4; i++)
            s = sim.Step(100, 60, 0.25);

        Assert.Equal(1, s.CrankRevolutions);
        Assert.Equal(1024, s.CrankEventTime);
        Assert.Equal(1056, s.SpeedCentiKmh);
        Assert.InRange((int)s.AccumulatedTorque, 508, 510);
    }

    [Fact]
    public void RideSimulator_ZeroCadence_KeepsCountersStill()
    {
        var sim = new RideSimulator();
        var s = sim.Step(0, 0, 0.25);
        Assert.Equal(0, s.CrankRevolutions);
        Assert.Equal(0u, s.WheelRevolutions);
        Assert.Equal(0, s.SpeedCentiKmh);
    }

    [Fact]
    public void RideKeyPad_AnalogEntries_MatchCaptureLayout()
    {
        // neutral frame from the capture: both axes explicitly 0
        Assert.Equal("2308FFFFFFFF0F1A04080010001A0408011000",
            Hex(ZwiftControlProtocol.BuildRideKeyPadFrame(ZwiftRideButton.None, new RideAnalogEntry(0, 0), new RideAnalogEntry(1, 0))));
        // capture: loc 0 = -32768 (65535 zigzag), loc 1 = 0
        Assert.Equal("2308FFFFFFFF0F1A06080010FFFF031A0408011000",
            Hex(ZwiftControlProtocol.BuildRideKeyPadFrame(ZwiftRideButton.None, new RideAnalogEntry(0, -32768), new RideAnalogEntry(1, 0))));
        // capture: loc 0 = 0, loc 1 = +32767 (65534 zigzag)
        Assert.Equal("2308FFFFFFFF0F1A04080010001A06080110FEFF03",
            Hex(ZwiftControlProtocol.BuildRideKeyPadFrame(ZwiftRideButton.None, new RideAnalogEntry(0, 0), new RideAnalogEntry(1, 32767))));
        // Zwift Ride style full paddle: left -100 (zigzag 199 = c7 01)
        Assert.Equal("2308FFFFFFFF0F1A050800" + "10C701",
            Hex(ZwiftControlProtocol.BuildRideKeyPadFrame(ZwiftRideButton.None, new RideAnalogEntry(0, -100))));
    }

    [Fact]
    public void Gatt_ServiceListsPerKind()
    {
        var id = DeviceIdentity.Default;
        Func<GattKind, BleUuid[]> services = g => DeviceProfiles.CreateServices(new DeviceProfileOptions(AdvertKind.Kickr, g), id).Select(s => s.Uuid).ToArray();
        Assert.Equal(new[]
        {
            DirconUuids.DeviceInformation, DirconUuids.WahooEE01, DirconUuids.CyclingPower, DirconUuids.FitnessMachine, DirconUuids.UserData,
            DirconUuids.WahooEE0D, DirconUuids.WahooEE0B, DirconUuids.WahooEE06, DirconUuids.ZwiftService,
        }, services(GattKind.Full));
        Assert.Equal(new[]
        {
            DirconUuids.DeviceInformation, DirconUuids.CyclingPower, DirconUuids.FitnessMachine, DirconUuids.UserData, DirconUuids.ZwiftService,
        }, services(GattKind.NoWahoo));
        Assert.Equal(new[] { DirconUuids.DeviceInformation, DirconUuids.ZwiftService }, services(GattKind.Minimal));
    }

    [Fact]
    public void Advert_TxtPerKind()
    {
        Assert.Equal("0xFC82,0x1818,0x1826,00000001-19CA-4651-86E5-FA29DCDD09D1,A026EE0D-0A7D-4AB3-97FA-F1500F9FEB8B", DeviceProfiles.BleServiceUuids(AdvertKind.Kickr));
        Assert.Equal("0x1826,0xFC82,00000001-19CA-4651-86E5-FA29DCDD09D1", DeviceProfiles.BleServiceUuids(AdvertKind.Ftms));
        Assert.Equal("0xFC82,00000001-19CA-4651-86E5-FA29DCDD09D1", DeviceProfiles.BleServiceUuids(AdvertKind.Zwift));
    }

    [Fact]
    public void Options_NamesManufacturerAndPresets()
    {
        var id = DeviceIdentity.Default;
        Assert.Equal("KICKR BIKE SHIFT GC4Z", DeviceProfiles.DefaultName(new DeviceProfileOptions()));
        Assert.Equal("Voltage Gamepad", DeviceProfiles.DefaultName(new DeviceProfileOptions(AdvertKind.Kickr, GattKind.NoWahoo)));
        Assert.Equal("Voltage Gamepad", DeviceProfiles.DefaultName(DeviceProfileOptions.FromKind(DeviceProfileKind.Zwift)));
        Assert.Equal(AdvertKind.Ftms, DeviceProfileOptions.FromKind(DeviceProfileKind.Ftms).Advert);
        Assert.Equal(GattKind.Minimal, DeviceProfileOptions.FromKind(DeviceProfileKind.Zwift).Gatt);
        foreach (var gatt in new[] { GattKind.Full, GattKind.NoWahoo, GattKind.Minimal })
        {
            var info = DeviceProfiles.CreateServices(new DeviceProfileOptions(AdvertKind.Kickr, gatt), id)[0];
            Assert.Equal("Wahoo Fitness", System.Text.Encoding.ASCII.GetString(info.Characteristics[0].Value));
            var custom = DeviceProfiles.CreateServices(new DeviceProfileOptions(AdvertKind.Kickr, gatt, true, "Foo"), id)[0];
            Assert.Equal("Foo", System.Text.Encoding.ASCII.GetString(custom.Characteristics[0].Value));
        }
    }
}
