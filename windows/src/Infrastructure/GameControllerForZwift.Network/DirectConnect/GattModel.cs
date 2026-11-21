namespace GameControllerForZwift.Network.DirectConnect;

/// <summary>GATT characteristic property bits as used in Direct Connect (DIRCON) characteristic discovery.</summary>
[Flags]
public enum GattProperties : byte
{
    /// <summary>No property.</summary>
    None = 0,
    /// <summary>The characteristic can be read.</summary>
    Read = 0x01,
    /// <summary>The characteristic can be written.</summary>
    Write = 0x02,
    /// <summary>The characteristic can notify.</summary>
    Notify = 0x04,
    /// <summary>The characteristic can indicate (DIRCON bit 3).</summary>
    Indicate = 0x08,
}

/// <summary>A GATT characteristic served over Direct Connect (DIRCON), with a static read value.</summary>
public sealed class GattCharacteristic
{
    /// <summary>Creates a characteristic.</summary>
    public GattCharacteristic(BleUuid uuid, GattProperties properties, byte[]? value = null)
    {
        Uuid = uuid;
        Properties = properties;
        Value = value ?? Array.Empty<byte>();
    }

    /// <summary>Characteristic UUID.</summary>
    public BleUuid Uuid { get; }

    /// <summary>Property bits as reported in characteristic discovery.</summary>
    public GattProperties Properties { get; }

    /// <summary>Value returned to read requests.</summary>
    public byte[] Value { get; set; }
}

/// <summary>A GATT service served over Direct Connect (DIRCON) and its characteristics (in discovery order).</summary>
public sealed class GattService
{
    /// <summary>Creates a service.</summary>
    public GattService(BleUuid uuid, IEnumerable<GattCharacteristic>? characteristics = null)
    {
        Uuid = uuid;
        Characteristics = characteristics?.ToList() ?? new List<GattCharacteristic>();
    }

    /// <summary>Service UUID.</summary>
    public BleUuid Uuid { get; }

    /// <summary>Characteristics in discovery order.</summary>
    public IReadOnlyList<GattCharacteristic> Characteristics { get; }
}
