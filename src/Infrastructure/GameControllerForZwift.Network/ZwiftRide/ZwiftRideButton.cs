namespace GameControllerForZwift.Network.ZwiftRide;

/// <summary>
/// Logical Zwift Ride buttons, named as in Zwift's own labels. Values are the "pressed" masks; on the wire the keypad
/// bitmap is the bitwise inverse (a pressed button clears its bit). Confirmed with live tests and Zwift's
/// shift-style-and-gear-range documentation (support.zwift.com): this is the BikeControl table.
/// </summary>
[Flags]
public enum ZwiftRideButton : uint
{
    /// <summary>No button pressed.</summary>
    None = 0,
    /// <summary>D-pad left.</summary>
    Left = 0x0001,
    /// <summary>D-pad up (action bar).</summary>
    Up = 0x0002,
    /// <summary>D-pad right.</summary>
    Right = 0x0004,
    /// <summary>D-pad down (U-turn).</summary>
    Down = 0x0008,
    /// <summary>A button (select).</summary>
    A = 0x0010,
    /// <summary>B button (back).</summary>
    B = 0x0020,
    /// <summary>Y button: use power-up; hold to skip the current workout block.</summary>
    Y = 0x0040,
    /// <summary>Z button: Ride On; hold to send Ride On to 5 riders.</summary>
    Z = 0x0080,
    /// <summary>Left shifter 1. Easier gear in sequential shift style.</summary>
    LS1 = 0x0100,
    /// <summary>Left shifter 2. Easier gear in sequential shift style.</summary>
    LS2 = 0x0200,
    /// <summary>Left side (drop) button.</summary>
    LB = 0x0400,
    /// <summary>Left power button.</summary>
    LPower = 0x0800,
    /// <summary>Right shifter 1. Harder gear in sequential shift style.</summary>
    RS1 = 0x1000,
    /// <summary>Right shifter 2. Harder gear in sequential shift style.</summary>
    RS2 = 0x2000,
    /// <summary>Right side (drop) button.</summary>
    RB = 0x4000,
    /// <summary>Right power button.</summary>
    RPower = 0x8000,
}
