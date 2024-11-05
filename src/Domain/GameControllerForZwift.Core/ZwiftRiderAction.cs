using System.ComponentModel;

namespace GameControllerForZwift.Core
{
    public enum ZwiftRiderAction
    {
        [Description("Stick out elbow")]
        Elbow = 1,
        [Description("Wave hand")]
        WaveHand = 2,
        [Description("Give a 'Ride On!'")]
        RideOn = 3,
        [Description("Say 'Hammer Time!'")]
        HammerTime = 4,
        [Description("Say 'Nice!'")]
        Nice = 5,
        [Description("Say 'Bring It!'")]
        BringIt = 6,
        [Description("Say 'I'm toast!'")]
        ImToast = 7,
        [Description("Ring the bike bell")]
        BikeBell = 8,
        [Description("Take a screen shot")]
        ScreenShot = 9
    }
}
