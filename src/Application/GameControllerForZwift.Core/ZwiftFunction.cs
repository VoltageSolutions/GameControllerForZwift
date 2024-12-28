using System.ComponentModel;

namespace GameControllerForZwift.Core
{
    public enum ZwiftFunction
    {
        [Description("No Action Configured")]
        None,
        [Description("Up / Show the in-ride menu")]
        ShowMenu,
        [Description("Left")]
        NavigateLeft,
        [Description("Right")]
        NavigateRight,
        [Description("Down / Make a U-Turn")]
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
        [Description("Hide the watt and HR graphs")]
        ToggleGraphs,
        [Description("Send a group text")]
        SendGroupText,
        [Description("Hide the Heads-Up Display")]
        HideHUD,
        [Description("Enter a Promo Code")]
        PromoCode
    }
}
