using GameControllerForZwift.Network.DirectConnect;
using GameControllerForZwift.Network.Mdns;
using GameControllerForZwift.Network.ZwiftRide;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using ZwiftLanProbe;

// ZwiftLanProbe: runs the Zwift Ride Direct Connect (DIRCON) emulator + mDNS advertiser standalone, so buttons and paddles can be tried
// against Zwift from the keyboard. It offers the same single Zwift Ride presentation as the app (ZwiftRideProfile).

string? nameArg = null;
string? logFile = null;
int port = 36866;
int partialValue = 50;
int holdMs = 550;
bool verbose = false;
for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--name" when i + 1 < args.Length: nameArg = args[++i]; break;
        case "--port" when i + 1 < args.Length:
            if (!int.TryParse(args[++i], out port) || port is < 1 or > 65535)
            {
                Console.WriteLine("--port must be 1..65535");
                return 1;
            }
            break;
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
        case "--log-file" when i + 1 < args.Length: logFile = args[++i]; break;
        case "--verbose": verbose = true; break;
        default:
            Console.WriteLine("Usage: ZwiftLanProbe [--name \"<name>\"] [--port N] [--partial 1..100] [--hold-ms N] [--log-file <path>] [--verbose]");
            return 1;
    }
}

string name = nameArg ?? "Voltage Gamepad";

// Ctrl+C = clean shutdown (the main loop polls this token).
using var stopSignal = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    try { stopSignal.Cancel(); } catch (ObjectDisposedException) { }
};

using var factory = LoggerFactory.Create(b =>
{
    b.SetMinimumLevel(LogLevel.Debug);
    b.AddSimpleConsole(o =>
    {
        o.SingleLine = true;
        o.TimestampFormat = "HH:mm:ss.fff ";
    });
    b.AddFilter<ConsoleLoggerProvider>(level => level >= (verbose ? LogLevel.Debug : LogLevel.Information));
    if (logFile is not null)
        b.AddProvider(new FileLogProvider(logFile));
});
var identity = DeviceIdentity.Default with { Name = name };
await using var server = new DirectConnectServer(factory.CreateLogger<DirectConnectServer>(), port);
await using var mdns = new MdnsAdvertiser(factory.CreateLogger<MdnsAdvertiser>());

server.ConnectionChanged += (_, connected) =>
    Console.WriteLine(connected ? ">>> Zwift connected, RideOn handshake complete." : ">>> Zwift disconnected.");

await server.StartAsync();
mdns.Start(identity, server.Port);

void PrintHelp()
{
    Console.WriteLine($"Device '{name}' port {server.Port}; Zwift Ride profile; logs: {logFile ?? "(console only, --log-file <path> to also write a file)"}");
    Console.WriteLine();
    Console.WriteLine("Key         Action                                              Value");
    Console.WriteLine($"=           RS1 tap (harder gear), 150 ms                       0x{(uint)ZwiftRideButton.RS1:X}");
    Console.WriteLine($"0           RS2 tap (harder gear)                               0x{(uint)ZwiftRideButton.RS2:X}");
    Console.WriteLine($"-           LS1 tap (easier gear)                               0x{(uint)ZwiftRideButton.LS1:X}");
    Console.WriteLine($"9           LS2 tap (easier gear)                               0x{(uint)ZwiftRideButton.LS2:X}");
    Console.WriteLine($"[           LB tap (left side button)                           0x{(uint)ZwiftRideButton.LB:X}");
    Console.WriteLine($"]           RB tap (right side button)                          0x{(uint)ZwiftRideButton.RB:X}");
    Console.WriteLine("q           Steer full left   (ZL/ZR tilt outward)               ZL (loc0) = -100");
    Console.WriteLine($"w           Steer partial left                                ZL (loc0) = -{partialValue}");
    Console.WriteLine($"e           Steer partial right                                 ZR (loc1) = +{partialValue}");
    Console.WriteLine("r           Steer full right                                    ZR (loc1) = +100");
    Console.WriteLine("a           Brake left full  (paddles inward)                   ZL (loc0) = +100");
    Console.WriteLine($"s           Brake left partial                                   ZL (loc0) = +{partialValue}");
    Console.WriteLine($"d           Brake right partial                                  ZR (loc1) = -{partialValue}");
    Console.WriteLine("f           Brake right full                                    ZR (loc1) = -100");
    Console.WriteLine($"            (steer/brake: hold the key; auto-repeat keeps it, released {holdMs} ms after the last repeat; --partial N, --hold-ms N)");
    Console.WriteLine($"Arrows      Left / Up / Right / Down buttons, tap               0x{(uint)ZwiftRideButton.Left:X} / 0x{(uint)ZwiftRideButton.Up:X} / 0x{(uint)ZwiftRideButton.Right:X} / 0x{(uint)ZwiftRideButton.Down:X}");
    Console.WriteLine($"Enter       A (select)                                          0x{(uint)ZwiftRideButton.A:X}");
    Console.WriteLine($"Backspace   B (back)                                            0x{(uint)ZwiftRideButton.B:X}");
    Console.WriteLine($"y / Y       Y tap (power-up) / Y hold 1.5 s (skip workout block) 0x{(uint)ZwiftRideButton.Y:X}");
    Console.WriteLine($"z / Z       Z tap (Ride On) / Z hold 1.5 s (Ride On to 5 riders)  0x{(uint)ZwiftRideButton.Z:X}");
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
            await server.SendKeypadFrameAsync(frame, ct);
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

async Task TapAsync(string label, ZwiftRideButton button, int ms = 150)
{
    uint mask = (uint)button;
    if (!server.IsZwiftConnected)
        Console.WriteLine("(warning: Zwift handshake not complete; frames are only sent if a client is connected)");
    Console.WriteLine($"{(ms > 150 ? "hold" : "tap")} {label} mask=0x{mask:X} ({ms} ms)");
    lock (gate) heldButtons |= mask;
    Wake();
    await Task.Delay(ms);
    lock (gate) heldButtons &= ~mask;
    Wake();
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
        await server.SendKeypadFrameAsync(frame);
        await Task.Delay(100);
    }
    await server.SendKeypadFrameAsync(ZwiftControlProtocol.BuildRideKeyPadFrame(ZwiftRideButton.None,
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
    await server.SendKeypadFrameAsync(ZwiftControlProtocol.BuildKeyPadFrame((ZwiftRideButton)mask));
    await Task.Delay(ms);
    await server.SendKeypadFrameAsync(ZwiftControlProtocol.BuildKeyPadFrame(ZwiftRideButton.None));
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
        _ = TapAsync(navLabel, Enum.Parse<ZwiftRideButton>(navLabel));
        continue;
    }
    switch (c)
    {
        case '=': _ = TapAsync("RS1 (harder)", ZwiftRideButton.RS1); break;
        case '0': _ = TapAsync("RS2 (harder)", ZwiftRideButton.RS2); break;
        case '-': _ = TapAsync("LS1 (easier)", ZwiftRideButton.LS1); break;
        case '9': _ = TapAsync("LS2 (easier)", ZwiftRideButton.LS2); break;
        case '[': _ = TapAsync("LB", ZwiftRideButton.LB); break;
        case ']': _ = TapAsync("RB", ZwiftRideButton.RB); break;
        case 'y': _ = TapAsync("Y", ZwiftRideButton.Y); break;
        case 'Y': _ = TapAsync("Y (hold 1.5 s)", ZwiftRideButton.Y, 1500); break;
        case 'z': _ = TapAsync("Z", ZwiftRideButton.Z); break;
        case 'Z': _ = TapAsync("Z (hold 1.5 s)", ZwiftRideButton.Z, 1500); break;
        case 'q': HoldPad("steer full left", 0, -100); break;
        case 'w': HoldPad("steer partial left", 0, -partialValue); break;
        case 'e': HoldPad("steer partial right", 1, partialValue); break;
        case 'r': HoldPad("steer full right", 1, 100); break;
        case 'a': HoldPad("brake left full", 0, 100); break;
        case 's': HoldPad("brake left partial", 0, partialValue); break;
        case 'd': HoldPad("brake right partial", 1, -partialValue); break;
        case 'f': HoldPad("brake right full", 1, -100); break;
        case 'i':
            Console.WriteLine($"zwiftConnected={server.IsZwiftConnected} port={server.Port} name='{name}' partial={partialValue} hold-ms={holdMs}");
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
try { await server.SendKeypadFrameAsync(BuildStateFrame(0, 0, 0)); } catch (Exception) { }
mdns.Stop(); // sends the mDNS goodbye (TTL 0) for our records
await server.StopAsync();
return 0;
