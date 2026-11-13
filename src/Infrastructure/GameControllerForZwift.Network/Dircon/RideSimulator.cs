namespace GameControllerForZwift.Network.Dircon;

/// <summary>Snapshot of simulated ride values for one notification tick.</summary>
public readonly record struct RideSample(
    int PowerWatts,
    int CadenceRpm,
    int SpeedCentiKmh,
    ushort AccumulatedTorque,
    uint WheelRevolutions,
    ushort WheelEventTime,
    ushort CrankRevolutions,
    ushort CrankEventTime)
{
    /// <summary>Speed in cm/s, used for field 3 of the 0x03 ride-data frame (capture: 358 at 12.67 km/h).</summary>
    public int SpeedCentimetresPerSecond => (int)Math.Round(SpeedCentiKmh / 3.6);

    /// <summary>Field 5 of the 0x03 frame: about 28.6 x field 3 in the capture (wheel rpm x 100).</summary>
    public int WheelRpmTimes100 => (int)Math.Round(SpeedCentimetresPerSecond * 28.6);
}

/// <summary>
/// Generates consistent cumulative crank/wheel counters for a constant power and cadence.
/// Relationships taken from the capture: speed is about 17.6 x cadence (0.01 km/h units), wheel circumference about 2.105 m,
/// accumulated torque grows by power x dt x 32 / (2 pi) (1/32 Nm units), event times are the time of the last full revolution
/// in 1/1024 s (crank) and 1/2048 s (wheel).
/// </summary>
public sealed class RideSimulator
{
    private const double WheelCircumferenceMetres = 2.105;
    private const double SpeedCentiKmhPerRpm = 17.6;

    private double _time;
    private double _crankRevs;
    private double _wheelRevs;
    private double _torque;
    private double _lastCrankEvent;
    private double _lastWheelEvent;

    /// <summary>Advances the simulation by <paramref name="dtSeconds"/> and returns the new sample.</summary>
    public RideSample Step(int powerWatts, int cadenceRpm, double dtSeconds)
    {
        powerWatts = Math.Max(0, powerWatts);
        cadenceRpm = Math.Max(0, cadenceRpm);
        if (dtSeconds < 0)
            dtSeconds = 0;
        _time += dtSeconds;

        int speed = (int)Math.Round(cadenceRpm * SpeedCentiKmhPerRpm);

        double crankRate = cadenceRpm / 60.0;
        if (crankRate > 0)
        {
            double before = _crankRevs;
            _crankRevs += crankRate * dtSeconds;
            if (Math.Floor(_crankRevs) > Math.Floor(before))
                _lastCrankEvent = _time - (_crankRevs - Math.Floor(_crankRevs)) / crankRate;
            _torque += powerWatts * dtSeconds * 32.0 / (2.0 * Math.PI);
        }

        double wheelRate = speed / 360.0 / WheelCircumferenceMetres; // m/s divided by metres per revolution
        if (wheelRate > 0)
        {
            double before = _wheelRevs;
            _wheelRevs += wheelRate * dtSeconds;
            if (Math.Floor(_wheelRevs) > Math.Floor(before))
                _lastWheelEvent = _time - (_wheelRevs - Math.Floor(_wheelRevs)) / wheelRate;
        }

        return new RideSample(
            powerWatts,
            cadenceRpm,
            speed,
            unchecked((ushort)((long)_torque & 0xFFFF)),
            unchecked((uint)(long)_wheelRevs),
            unchecked((ushort)((long)(_lastWheelEvent * 2048.0) & 0xFFFF)),
            unchecked((ushort)((long)_crankRevs & 0xFFFF)),
            unchecked((ushort)((long)(_lastCrankEvent * 1024.0) & 0xFFFF)));
    }
}
