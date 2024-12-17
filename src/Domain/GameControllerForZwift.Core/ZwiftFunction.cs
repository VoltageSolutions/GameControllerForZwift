using System.ComponentModel;

namespace GameControllerForZwift.Core
{
    public enum ZwiftFunction
    {
        [Description("No Action Configured")]
        None,
        [Description("Show the menu")]
        ShowMenu,
        [Description("Navigate left")]
        NavigateLeft,
        [Description("Navigate right")]
        NavigateRight,
        [Description("Make a U-Turn")]
        Uturn,
        [Description("Use a Power-up")]
        Powerup,
        [Description("Select")]
        Select,
        [Description("Go Back")]
        GoBack,
        [Description("Skip a workout block")]
        SkipWorkoutBlock,
        [Description("Increase the FTP Bias")]
        FTPBiasUp,
        [Description("Descease the FTP Bias")]
        FTPBiasDown,
        [Description("Adjust the camera angle")]
        AdjustCameraAngle,
        [Description("Rider (Function Key) action")]
        RiderAction,
        [Description("Show Paired Devices screen")]
        ShowPairedDevices,
        [Description("Show the Garage")]
        ShowGarage,
        [Description("Show the Training Menu")]
        ShowTrainingMenu,
        [Description("Toggle graphs")]
        ToggleGraphs,
        [Description("Send a group text")]
        SendGroupText,
        [Description("Hide the Heads-Up Display")]
        HideHUD,
        [Description("Enter a Promo Code")]
        PromoCode
    }
}
