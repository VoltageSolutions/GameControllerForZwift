using System.ComponentModel;

namespace GameControllerForZwift.Core
{
    public enum ZwiftPlayerView
    {
        [Description("Default View (Behind Rider)")]
        Default = 1,
        [Description("3rd-Person Perspective")]
        ThirdPerson = 2,
        [Description("1st-Person Perspective")]
        FPS = 3,
        [Description("From the front left-side")]
        FrontLeftSide = 4,
        [Description("From the rear right-side")]
        RearRightSide = 5,
        [Description("Reverse, facing rider")]
        FacingRider = 6,
        [Description("Spectator View")]
        Spectator = 7,
        [Description("Helicopter")]
        Helicopter = 8,
        [Description("Birds'-Eye View")]
        BirdsEye = 9,
        [Description("Drone")]
        Drone = 0
    }
}