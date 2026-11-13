using GameControllerForZwift.Network.Dircon;
using GameControllerForZwift.Network.Logging;
using GameControllerForZwift.Network.Mdns;
using Microsoft.Extensions.Logging;

// ZwiftLanProbe: runs the DirCon emulator + mDNS advertiser standalone so button masks can be tried against Zwift.

string? nameArg = null;
// Defaults = "--preset bcride" (BikeControl replica; verified: Zwift treats it as a Zwift Ride). Flags below override.
AdvertKind advert = AdvertKind.Kickr;
GattKind gatt = GattKind.BikeControlRide;
bool gearing = false;
bool addPowerService = false;
ControllerIdentity identityKind = ControllerIdentity.BikeControlRide;
string identityName = "bcride";
string manufacturer = DeviceProfiles.DefaultManufacturer;
int analogMax = 100;
int port = 36866;
string masks = "zwift";
int partialValue = 50;
int holdMs = 550;
bool verbose = false;
int maxGear = 5;
bool zpListen = false, zpOnly = false;
int zpPort = 36870;
string zpName = "Voltage Gamepad";
var zpExtraTxt = new List<string>();
string? mfgData = DeviceProfileOptions.MfgPreset("bikecontrol");
var txtExtra = new List<string>();
bool addObcUuid = true;
for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--name" when i + 1 < args.Length: nameArg = args[++i]; break;
        case "--profile" when i + 1 < args.Length:
            if (!Enum.TryParse(args[++i], true, out DeviceProfileKind kind) || !Enum.IsDefined(kind))
            {
                Console.WriteLine("--profile must be kickr, ftms or zwift");
                return 1;
            }
            var preset = DeviceProfileOptions.FromKind(kind);
            advert = preset.Advert; gatt = preset.Gatt; gearing = preset.EnableWahooGearing;
            break;
        case "--advert" when i + 1 < args.Length:
            if (!Enum.TryParse(args[++i], true, out advert) || !Enum.IsDefined(advert))
            {
                Console.WriteLine("--advert must be kickr, ftms or zwift");
                return 1;
            }
            break;
        case "--gatt" when i + 1 < args.Length:
            string gattArg = args[++i];
            if (string.Equals(gattArg, "bcride", StringComparison.OrdinalIgnoreCase))
                gatt = GattKind.BikeControlRide;
            else if (!Enum.TryParse(gattArg, true, out gatt) || !Enum.IsDefined(gatt))
            {
                Console.WriteLine("--gatt must be full, nowahoo, minimal or bcride");
                return 1;
            }
            break;
        case "--preset" when i + 1 < args.Length:
            string presetArg = args[++i];
            if (string.Equals(presetArg, "bcride", StringComparison.OrdinalIgnoreCase))
            {
                // BikeControl replica (the default): put --preset first, later flags (--mfg, --name, --manufacturer, ...) override it.
                var ridePreset = DeviceProfiles.ZwiftRidePreset();
                advert = ridePreset.Advert; gatt = ridePreset.Gatt; gearing = ridePreset.EnableWahooGearing;
                identityKind = ridePreset.Controller; identityName = "bcride";
                mfgData = ridePreset.MfgData;
                addObcUuid = ridePreset.AddObcUuid;
            }
            else if (string.Equals(presetArg, "kickr", StringComparison.OrdinalIgnoreCase))
            {
                // The old default: emulated KICKR BIKE with the full Wahoo GATT tree and gear reports.
                advert = AdvertKind.Kickr; gatt = GattKind.Full; gearing = true;
                identityKind = ControllerIdentity.KickrBike; identityName = "kickr";
                mfgData = null;
                addObcUuid = false;
            }
            else
            {
                Console.WriteLine("--preset must be bcride (default) or kickr");
                return 1;
            }
            break;
        case "--identity" when i + 1 < args.Length:
            identityName = args[++i].ToLowerInvariant();
            switch (identityName)
            {
                case "kickr": identityKind = ControllerIdentity.KickrBike; break;
                case "ride": identityKind = ControllerIdentity.Ride; break;
                case "clickv2": identityKind = ControllerIdentity.ClickV2; break;
                case "clickv1": identityKind = ControllerIdentity.ClickV1; break;
                case "play": identityKind = ControllerIdentity.Play; break;
                case "bcride": identityKind = ControllerIdentity.BikeControlRide; break;
                default:
                    Console.WriteLine("--identity must be kickr, ride, clickv2, clickv1, play or bcride");
                    return 1;
            }
            break;
        case "--manufacturer" when i + 1 < args.Length: manufacturer = args[++i]; break;
        case "--no-gearing": gearing = false; break;
        case "--analog-max" when i + 1 < args.Length: analogMax = Math.Clamp(int.Parse(args[++i]), 1, 32767); break;
        case "--port" when i + 1 < args.Length: port = int.Parse(args[++i]); break;
        case "--masks" when i + 1 < args.Length: masks = args[++i].ToLowerInvariant(); break;
        case "--partial" when i + 1 < args.Length:
            if (!int.TryParse(args[++i], out partialValue) || partialValue is < 1 or > 100)
            {
                Console.WriteLine("--partial must be 1..100");
                return 1;
            }
            break;
        case "--hold-ms" when i + 1 < args.Length:
            if (!int.TryParse(args[++i], out holdMs) || holdMs < 150)
            {
                Console.WriteLine("--hold-ms must be a number >= 150");
                return 1;
            }
            break;
        case "--verbose": verbose = true; break;
        case "--max-gear" when i + 1 < args.Length: maxGear = Math.Max(2, int.Parse(args[++i])); break;
        case "--zp-listen": zpListen = true; break;
        case "--zp-only": zpOnly = true; break;
        case "--zp-port" when i + 1 < args.Length: zpPort = int.Parse(args[++i]); break;
        case "--mfg-data" when i + 1 < args.Length: mfgData = args[++i].Trim(); break;
        case "--mfg" when i + 1 < args.Length:
            mfgData = DeviceProfileOptions.MfgPreset(args[++i]);
            if (mfgData == null)
            {
                Console.WriteLine("--mfg must be ride, clickv1, clickv2, play or bikecontrol");
                return 1;
            }
            break;
        case "--txt-extra" when i + 1 < args.Length:
            txtExtra.AddRange(args[++i].Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
            break;
        case "--add-obc-uuid": addObcUuid = true; break;
        case "--add-power-service": addPowerService = true; break;
        case "--zp-name" when i + 1 < args.Length: zpName = args[++i]; break;
        case "--zp-txt" when i + 1 < args.Length:
            zpExtraTxt.AddRange(args[++i].Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
            break;
        default:
            Console.WriteLine("Usage: ZwiftLanProbe [--name \"<name>\"] [--profile kickr|ftms|zwift] [--advert kickr|ftms|zwift] [--preset bcride|kickr] [--gatt full|nowahoo|minimal|bcride] [--identity kickr|ride|clickv2|clickv1|play|bcride] [--manufacturer \"<s>\"] [--mfg ride|clickv1|clickv2|play|bikecontrol] [--mfg-data <hex>] [--txt-extra \"k=v;k2=v2\"] [--add-obc-uuid] [--add-power-service] [--no-gearing] [--port N] [--masks zwift|makinolo] [--partial 1..100] [--hold-ms N] [--max-gear N] [--analog-max N] [--verbose] [--zp-listen [--zp-port N] [--zp-name \"<name>\"] [--zp-txt \"k=v;k2=v2\"] [--zp-only]]");
            return 1;
    }
}
if (zpOnly && !zpListen)
{
    Console.WriteLine("--zp-only requires --zp-listen");
    return 1;
}
if (masks is not ("zwift" or "makinolo"))
{
    Console.WriteLine("--masks must be zwift or makinolo");
    return 1;
}
bool makinolo = masks == "makinolo"; // reference only: Makinolo upper values are wrong for Zwift
var profile = new DeviceProfileOptions(advert, gatt, gearing, manufacturer, identityKind, mfgData, txtExtra, addObcUuid, addPowerService);
string name = nameArg ?? DeviceProfiles.DefaultName(profile);

const int MinGear = 1;
const int StartGear = 4; // first 0x42 gear value seen in the capture (observed range 1..5)
int gear = Math.Min(StartGear, maxGear);
int simWatts = 100, simRpm = 80;

// label, Makinolo mask (reference only, wrong for Zwift's upper bits), Zwift mask (= ZwiftRideButton, confirmed)
var table = new (string Label, uint Mako, uint Zwift)[]
{
    ("Left", 0x1, (uint)ZwiftRideButton.Left),
    ("Up", 0x2, (uint)ZwiftRideButton.Up),
    ("Right", 0x4, (uint)ZwiftRideButton.Right),
    ("Down", 0x8, (uint)ZwiftRideButton.Down),
    ("A", 0x10, (uint)ZwiftRideButton.A),
    ("B", 0x20, (uint)ZwiftRideButton.B),
    ("Y", 0x40, (uint)ZwiftRideButton.Y),
    ("Z", 0x100, (uint)ZwiftRideButton.Z),
    ("LS1", 0x200, (uint)ZwiftRideButton.LS1),
    ("LS2", 0x400, (uint)ZwiftRideButton.LS2),
    ("LB", 0x400, (uint)ZwiftRideButton.LB),
    ("LPower", 0x800, (uint)ZwiftRideButton.LPower),
    ("RS1", 0x2000, (uint)ZwiftRideButton.RS1),
    ("RS2", 0x4000, (uint)ZwiftRideButton.RS2),
    ("RB", 0x4000, (uint)ZwiftRideButton.RB),
    ("RPower", 0x10000, (uint)ZwiftRideButton.RPower),
};
uint MaskFor(string label)
{
    var entry = table.First(e => e.Label == label);
    return makinolo ? entry.Mako : entry.Zwift;
}

// Ctrl+C = clean shutdown (the main loop polls this token).
using var stopSignal = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    try { stopSignal.Cancel(); } catch (ObjectDisposedException) { }
};

var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
using var factory = LoggerFactory.Create(b =>
{
    b.SetMinimumLevel(LogLevel.Debug);
    b.AddProvider(new SimpleConsoleLoggerProvider(verbose ? LogLevel.Debug : LogLevel.Information));
    b.AddFileLogger(logDir);
});
var identity = DeviceIdentity.Default with { Name = name };
await using var server = new DirconServer(identity, factory.CreateLogger<DirconServer>(), port, null, profile);
await using var mdns = new MdnsAdvertiser(factory.CreateLogger<MdnsAdvertiser>(), profile);

server.ConnectionChanged += (_, connected) =>
    Console.WriteLine(connected ? ">>> Zwift connected, RideOn handshake complete." : ">>> Zwift disconnected.");

await server.StartAsync();
ZpListener? zp = null;
if (zpListen)
{
    var zpTxt = new List<string>
    {
        "ble-service-uuids=0xFC82,00000001-19CA-4651-86E5-FA29DCDD09D1",
        "mac-address=" + identity.MacAddress,
        "serial-number=" + identity.SerialNumber,
    };
    foreach (var kv in zpExtraTxt)
    {
        int eq = kv.IndexOf('=');
        string key = eq < 0 ? kv : kv[..eq];
        zpTxt.RemoveAll(e => e.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase) || string.Equals(e, key, StringComparison.OrdinalIgnoreCase));
        zpTxt.Add(kv);
    }
    zp = new ZpListener(identity, profile, factory.CreateLogger<ZpListener>(), zpPort);
    zp.Start();
    var zpExtra = new MdnsExtraService(zpName, "_zwift-protocol._tcp", zp.Port, zpTxt);
    mdns.Start(identity, server.Port, zpExtra, !zpOnly);
    Console.WriteLine($"ZP mode: advertising '{zpName}' _zwift-protocol._tcp on TCP {zp.Port}, TXT [{string.Join("; ", zpTxt)}]{(zpOnly ? "; ONLY zp (no _wahoo-fitness-tnp)" : "; plus _wahoo-fitness-tnp")}");
}
else
    mdns.Start(identity, server.Port);

void PrintHelp()
{
    Console.WriteLine($"Device '{name}' port {server.Port}; advert={advert} (TXT ble-service-uuids={DeviceProfiles.BleServiceUuids(advert)}) gatt={gatt} identity={identityName} manufacturer='{manufacturer}' mfg-data={(mfgData ?? "(none)")} txt-extra=[{string.Join(";", txtExtra)}] obc-uuid={addObcUuid} power-service={addPowerService} gearing={(gearing ? "on" : "off")}; mask table: {masks}; logs: {logDir}");
    Console.WriteLine(zpListen
        ? $"ZP mode: ON ({(zpOnly ? "zp-only" : "both adverts")}), name '{zpName}', TCP {zp?.Port}; watch for 'ZP' lines"
        : "ZP mode: off (--zp-listen to enable)");
    Console.WriteLine();
    Console.WriteLine("Key         Action                                              Value");
    Console.WriteLine($"=           RS1 tap (harder gear), 150 ms                       0x{MaskFor("RS1"):X}");
    Console.WriteLine($"0           RS2 tap (harder gear)                               0x{MaskFor("RS2"):X}");
    Console.WriteLine($"-           LS1 tap (easier gear)                               0x{MaskFor("LS1"):X}");
    Console.WriteLine($"9           LS2 tap (easier gear)                               0x{MaskFor("LS2"):X}");
    Console.WriteLine($"q           Steer full left   (ZL/ZR tilt outward)               ZL (loc0) = -100");
    Console.WriteLine($"w           Steer partial left                                ZL (loc0) = -{partialValue}");
    Console.WriteLine($"e           Steer partial right                                 ZR (loc1) = +{partialValue}");
    Console.WriteLine($"r           Steer full right                                    ZR (loc1) = +100");
    Console.WriteLine($"a           Brake left full  (paddles inward)                   ZL (loc0) = +100");
    Console.WriteLine($"s           Brake left partial                                   ZL (loc0) = +{partialValue}");
    Console.WriteLine($"d           Brake right partial                                  ZR (loc1) = -{partialValue}");
    Console.WriteLine($"f           Brake right full                                    ZR (loc1) = -100");
    Console.WriteLine($"            (steer/brake: hold the key; auto-repeat keeps it, released {holdMs} ms after the last repeat; --partial N, --hold-ms N)");
    Console.WriteLine($"Arrows      Left / Up / Right / Down buttons, tap               0x{MaskFor("Left"):X} / 0x{MaskFor("Up"):X} / 0x{MaskFor("Right"):X} / 0x{MaskFor("Down"):X}");
    Console.WriteLine($"Enter       A (select)                                          0x{MaskFor("A"):X}");
    Console.WriteLine($"Backspace   B (back)                                            0x{MaskFor("B"):X}");
    Console.WriteLine($"y / Y       Y tap (power-up) / Y hold 1.5 s (skip workout block) 0x{MaskFor("Y"):X}");
    Console.WriteLine($"z / Z       Z tap (Ride On) / Z hold 1.5 s (Ride On to 5 riders)  0x{MaskFor("Z"):X}");
    Console.WriteLine($"[           LB tap (left side button)                           0x{MaskFor("LB"):X}");
    Console.WriteLine($"]           RB tap (right side button)                          0x{MaskFor("RB"):X}");
    Console.WriteLine("p           Toggle simulated ride                               100 W / 80 rpm");
    Console.WriteLine("P           Simulated ride, prompts 'watts,rpm'");
    Console.WriteLine("i           Status");
    Console.WriteLine("?           This help");
    Console.WriteLine("#           Press raw bit: index 0-31 then Enter (mask 1<<n; reaches LPower/RPower)");
    Console.WriteLine("@           Analog lab: prompt 'loc,value[,ms[,buttonsHex]]', entries separated by ';'");
    Console.WriteLine("Ctrl+C      Clean shutdown (neutral frame, mDNS goodbye, stop server)");
    Console.WriteLine();
}

PrintHelp();

// ---- state loop: one background task owns every Ride keypad (0x23) frame for held paddles and tapped buttons ----
var gate = new object();
int padL = 0, padR = 0;
DateTime expL = DateTime.MinValue, expR = DateTime.MinValue;
uint heldButtons = 0;
bool wasActive = false;
using var wake = new SemaphoreSlim(0, int.MaxValue);
void Wake()
{
    if (wake.CurrentCount == 0) wake.Release();
}

byte[] BuildStateFrame(uint buttons, int left, int right)
    => ZwiftControlProtocol.BuildRideKeyPadFrame((ZwiftRideButton)buttons, new RideAnalogEntry(0, left), new RideAnalogEntry(1, right));

async Task StateLoopAsync(CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        bool woke;
        try { woke = await wake.WaitAsync(100, ct); }
        catch (OperationCanceledException) { break; }
        byte[]? frame = null;
        lock (gate)
        {
            var now = DateTime.UtcNow;
            if (padL != 0 && now >= expL) padL = 0;
            if (padR != 0 && now >= expR) padR = 0;
            bool pads = padL != 0 || padR != 0;
            bool active = pads || heldButtons != 0;
            // resend every 100 ms while a paddle is held; a state change sends at once; one neutral frame after the last release
            if (woke || active || (wasActive && !active))
                frame = BuildStateFrame(heldButtons, padL, padR);
            wasActive = active;
        }
        if (frame is not null)
            await server.SendAsyncFrameAsync(frame);
    }
}

void HoldPad(string label, int loc, int value)
{
    bool changed;
    lock (gate)
    {
        var exp = DateTime.UtcNow.AddMilliseconds(holdMs);
        if (loc == 0) { changed = padL != value; padL = value; expL = exp; }
        else { changed = padR != value; padR = value; expR = exp; }
    }
    if (changed)
    {
        if (!server.IsZwiftConnected)
            Console.WriteLine("(warning: Zwift handshake not complete; frames are only sent if a client is connected)");
        Console.WriteLine($"{label}: loc{loc} = {value:+#;-#;0}");
        Wake();
    }
}

async Task TapAsync(string label, uint mask, int ms = 150)
{
    if (!server.IsZwiftConnected)
        Console.WriteLine("(warning: Zwift handshake not complete; frames are only sent if a client is connected)");
    Console.WriteLine($"{(ms > 150 ? "hold" : "tap")} {label} mask=0x{mask:X} ({ms} ms)");
    lock (gate) heldButtons |= mask;
    Wake();
    await Task.Delay(ms);
    lock (gate) heldButtons &= ~mask;
    Wake();
}

async Task ClickAsync(bool plus)
{
    Console.WriteLine($"Click v1 {(plus ? "plus" : "minus")} press+release (0x37)");
    await server.SendAsyncFrameAsync(ZwiftControlProtocol.BuildClickKeyPadFrame(plus, !plus));
    await Task.Delay(150);
    await server.SendAsyncFrameAsync(ZwiftControlProtocol.BuildClickKeyPadFrame(false, false));
}

async Task PlayAsync(bool right)
{
    Console.WriteLine($"Play {(right ? "right" : "left")} paddle press+release (0x07)");
    await server.SendAsyncFrameAsync(ZwiftControlProtocol.BuildPlayKeyPadFrame(right, true));
    await Task.Delay(150);
    await server.SendAsyncFrameAsync(ZwiftControlProtocol.BuildPlayKeyPadFrame(right, false));
}

async Task ShiftGearAsync(int delta)
{
    gear = Math.Clamp(gear + delta, MinGear, maxGear);
    Console.WriteLine($"gear index -> {gear} (0x42)");
    await server.SendGearAsync(gear);
}

async Task HoldRepeatedAsync(string label, uint mask, int ms)
{
    Console.WriteLine($"hold {label} mask=0x{mask:X} for {ms} ms, frame every 100 ms");
    var end = DateTime.UtcNow.AddMilliseconds(ms);
    while (DateTime.UtcNow < end)
    {
        await server.SendButtonsAsync((ZwiftRideButton)mask);
        await Task.Delay(100);
    }
    await server.SendButtonsAsync(ZwiftRideButton.None);
    Console.WriteLine("release");
}

async Task PaddleAsync(bool right)
{
    int value = right ? analogMax : -analogMax; // the left paddle pulls negative
    Console.WriteLine($"{(right ? "right" : "left")} paddle full ({value}) for 1 s");
    var frame = ZwiftControlProtocol.BuildRideKeyPadFrame(ZwiftRideButton.None,
        new RideAnalogEntry(0, right ? 0 : value), new RideAnalogEntry(1, right ? value : 0));
    var end = DateTime.UtcNow.AddSeconds(1);
    while (DateTime.UtcNow < end)
    {
        await server.SendAsyncFrameAsync(frame);
        await Task.Delay(100);
    }
    await server.SendAsyncFrameAsync(ZwiftControlProtocol.BuildRideKeyPadFrame(ZwiftRideButton.None, new RideAnalogEntry(0, 0), new RideAnalogEntry(1, 0)));
    Console.WriteLine("neutral");
}

async Task AnalogAsync(string label, uint mask, RideAnalogEntry[] entries, int ms)
{
    if (!server.IsZwiftConnected)
        Console.WriteLine("(warning: Zwift handshake not complete; frame is only sent if a client is connected)");
    Console.WriteLine($"analog {label}: buttons=0x{mask:X} entries=[{string.Join(", ", entries.Select(e => $"loc{e.Location}={e.Value}"))}] for {ms} ms, every 100 ms");
    var frame = ZwiftControlProtocol.BuildRideKeyPadFrame((ZwiftRideButton)mask, entries);
    var end = DateTime.UtcNow.AddMilliseconds(ms);
    while (DateTime.UtcNow < end)
    {
        await server.SendAsyncFrameAsync(frame);
        await Task.Delay(100);
    }
    await server.SendAsyncFrameAsync(ZwiftControlProtocol.BuildRideKeyPadFrame(ZwiftRideButton.None,
        entries.Select(e => e.Location).Distinct().Select(l => new RideAnalogEntry(l, 0)).ToArray()));
    Console.WriteLine("neutral");
}

async Task CustomAnalogAsync()
{
    Console.Write("loc,value[,ms[,buttonsHex]] (';' separates entries, e.g. 0,-100;1,0): ");
    var input = Console.ReadLine() ?? "";
    var entries = new List<RideAnalogEntry>();
    int ms = 1500;
    uint mask = 0;
    foreach (var item in input.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
    {
        var f = item.Split(',', StringSplitOptions.TrimEntries);
        if (f.Length < 2 || !int.TryParse(f[0], out int loc) || loc < 0 || !int.TryParse(f[1], out int val))
        {
            Console.WriteLine($"invalid entry '{item}'");
            return;
        }
        entries.Add(new RideAnalogEntry(loc, val));
        if (f.Length >= 3 && int.TryParse(f[2], out int m) && m > 0) ms = m;
        if (f.Length >= 4 && uint.TryParse(f[3].Replace("0x", "", StringComparison.OrdinalIgnoreCase), System.Globalization.NumberStyles.HexNumber, null, out uint b)) mask = b;
    }
    if (entries.Count == 0)
    {
        Console.WriteLine("nothing entered");
        return;
    }
    await AnalogAsync("custom", mask, entries.ToArray(), ms);
}

async Task PressAsync(string label, uint mask, int ms)
{
    if (!server.IsZwiftConnected)
        Console.WriteLine("(warning: Zwift handshake not complete; frame is only sent if a client is connected)");
    Console.WriteLine($"press {label} mask=0x{mask:X} for {ms} ms");
    await server.SendButtonsAsync((ZwiftRideButton)mask);
    await Task.Delay(ms);
    await server.SendButtonsAsync(ZwiftRideButton.None);
    Console.WriteLine("release");
}

var stop = stopSignal.Token;
var stateLoop = StateLoopAsync(stop);
while (!stop.IsCancellationRequested)
{
    bool available;
    try { available = Console.KeyAvailable; }
    catch (InvalidOperationException) { available = true; } // redirected input: fall back to blocking reads
    if (!available)
    {
        await Task.Delay(10);
        continue;
    }
    var key = Console.ReadKey(true);
    char c = key.KeyChar;
    string? navLabel = key.Key switch
    {
        ConsoleKey.LeftArrow => "Left",
        ConsoleKey.UpArrow => "Up",
        ConsoleKey.RightArrow => "Right",
        ConsoleKey.DownArrow => "Down",
        ConsoleKey.Enter => "A",
        ConsoleKey.Backspace => "B",
        _ => null,
    };
    if (navLabel is not null)
    {
        _ = TapAsync(navLabel, MaskFor(navLabel));
        continue;
    }
    switch (c)
    {
        case '=': _ = TapAsync("RS1 (harder)", MaskFor("RS1")); break;
        case '0': _ = TapAsync("RS2 (harder)", MaskFor("RS2")); break;
        case '-': _ = TapAsync("LS1 (easier)", MaskFor("LS1")); break;
        case '9': _ = TapAsync("LS2 (easier)", MaskFor("LS2")); break;
        case '[': _ = TapAsync("LB", MaskFor("LB")); break;
        case ']': _ = TapAsync("RB", MaskFor("RB")); break;
        case 'y': _ = TapAsync("Y", MaskFor("Y")); break;
        case 'Y': _ = TapAsync("Y (hold 1.5 s)", MaskFor("Y"), 1500); break;
        case 'z': _ = TapAsync("Z", MaskFor("Z")); break;
        case 'Z': _ = TapAsync("Z (hold 1.5 s)", MaskFor("Z"), 1500); break;
        case 'q': HoldPad("steer full left", 0, -100); break;
        case 'w': HoldPad("steer partial left", 0, -partialValue); break;
        case 'e': HoldPad("steer partial right", 1, partialValue); break;
        case 'r': HoldPad("steer full right", 1, 100); break;
        case 'a': HoldPad("brake left full", 0, 100); break;
        case 's': HoldPad("brake left partial", 0, partialValue); break;
        case 'd': HoldPad("brake right partial", 1, -partialValue); break;
        case 'f': HoldPad("brake right full", 1, -100); break;
        case 'p':
            if (server.IsRideSimulated)
            {
                server.StopSimulatedRide();
                Console.WriteLine("simulated ride OFF");
            }
            else
            {
                simWatts = 100; simRpm = 80;
                server.SetSimulatedRide(simWatts, simRpm);
                Console.WriteLine($"simulated ride ON: {simWatts} W, {simRpm} rpm");
            }
            break;
        case 'P':
        {
            Console.Write("watts,rpm: ");
            var parts = (Console.ReadLine() ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && int.TryParse(parts[0], out int w) && int.TryParse(parts[1], out int rpm) && w >= 0 && rpm >= 0)
            {
                simWatts = w; simRpm = rpm;
                server.SetSimulatedRide(simWatts, simRpm);
                Console.WriteLine($"simulated ride ON: {simWatts} W, {simRpm} rpm");
            }
            else
                Console.WriteLine("invalid input, expected e.g. 150,90");
            break;
        }
        case 'i':
            Console.WriteLine($"zwiftConnected={server.IsZwiftConnected} port={server.Port} name='{name}' masks={masks} partial={partialValue} hold-ms={holdMs} ride={(server.IsRideSimulated ? $"{simWatts} W/{simRpm} rpm" : "off")}");
            break;
        case '?': PrintHelp(); break;
        case '@': await CustomAnalogAsync(); break;
        case '#':
            Console.Write("bit index 0-31: ");
            var line = Console.ReadLine();
            if (int.TryParse(line, out int bit) && bit is >= 0 and <= 31)
                await PressAsync($"raw bit {bit}", 1u << bit, 150);
            else
                Console.WriteLine("invalid bit index");
            break;
    }
}

Console.WriteLine("Ctrl+C: shutting down...");
try { await stateLoop; } catch (OperationCanceledException) { }
try { await server.SendAsyncFrameAsync(BuildStateFrame(0, 0, 0)); } catch (Exception) { }
mdns.Stop(); // sends the mDNS goodbye (TTL 0) for our records
if (zp is not null) await zp.DisposeAsync();
await server.StopAsync();
return 0;

/// <summary>Tiny console logger (no extra packages).</summary>
sealed class SimpleConsoleLoggerProvider(LogLevel min) : ILoggerProvider
{
    private static readonly object Gate = new();
    public ILogger CreateLogger(string categoryName) => new L(categoryName[(categoryName.LastIndexOf('.') + 1)..], min);
    public void Dispose() { }

    private sealed class L(string category, LogLevel min) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= min && logLevel != LogLevel.None;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            lock (Gate)
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {logLevel.ToString()[..3].ToUpperInvariant()} {category}: {formatter(state, exception)}");
                if (exception is not null) Console.WriteLine(exception);
            }
        }
    }
}
