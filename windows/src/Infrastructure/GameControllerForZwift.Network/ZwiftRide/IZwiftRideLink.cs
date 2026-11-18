namespace GameControllerForZwift.Network.ZwiftRide;

/// <summary>Link to Zwift that presents the app as a Zwift Ride controller (implemented over Wahoo Direct Connect, see DirectConnectServer).</summary>
public interface IZwiftRideLink : IAsyncDisposable
{
    /// <summary>TCP port in use (valid after <see cref="StartAsync"/>).</summary>
    int Port { get; }

    /// <summary>True after the RideOn handshake completed.</summary>
    bool IsZwiftConnected { get; }

    /// <summary>Raised with the new value of <see cref="IsZwiftConnected"/>.</summary>
    event EventHandler<bool>? ConnectionChanged;

    /// <summary>Binds the listener (dual-mode IPv6, falls back through port+10) and starts accepting.</summary>
    Task StartAsync(CancellationToken ct = default);

    /// <summary>Stops listening and closes the current session.</summary>
    Task StopAsync();

    /// <summary>
    /// Sends the Ride keypad frame (buttons plus the two analog paddles, ZL = location 0 and ZR = location 1, each -100..100) to the
    /// current session. The state is remembered and resent by the keepalive.
    /// </summary>
    Task SendInputAsync(ZwiftRideButton pressed, sbyte zl, sbyte zr, CancellationToken ct = default);

    /// <summary>Battery percentage (0..100, null = default 100) reported in the id-771 battery reply.</summary>
    void SetBatteryPercent(int? percent);
}
