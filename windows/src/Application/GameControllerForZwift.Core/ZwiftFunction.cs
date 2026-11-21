using System.ComponentModel;

namespace GameControllerForZwift.Core
{
    /// <summary>
    /// Exactly what a Zwift Ride controller provides. Profiles persist this enum by name (see <see cref="ZwiftFunctionJsonConverter"/>),
    /// so do not rename members; removed or unknown names load as <see cref="None"/>.
    /// </summary>
    // Hold behaviour is Zwift's: holding Powerup (Y) skips a workout block; holding RideOn (Z) sends Ride On bombs.
    public enum ZwiftFunction
    {
        [Description("No Action Configured")]
        None,
        [Description("Navigate Up")]
        NavigateUp,
        [Description("Navigate Down")]
        NavigateDown,
        [Description("Navigate Left")]
        NavigateLeft,
        [Description("Navigate Right")]
        NavigateRight,
        [Description("Select (A)")]
        Select,
        [Description("Back / Menu (B)")]
        GoBack,
        [Description("Power-up (Y)")]
        Powerup,
        [Description("Ride On (Z)")]
        RideOn,
        [Description("Shift up / Increase workout intensity")]
        ShiftUp,
        [Description("Shift down / Decrease workout intensity")]
        ShiftDown,
        [Description("Steer left")]
        SteerLeft,
        [Description("Steer right")]
        SteerRight,
        [Description("Brake")]
        Brake
    }
}
