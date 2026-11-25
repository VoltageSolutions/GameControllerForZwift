using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.UI.WPF.Localization;

namespace GameControllerForZwift.UI.WPF.Controls
{
    /// <summary>Gives the screen reader name of the controller illustration for each <see cref="ControllerKind"/> (SCR-BTN-047).</summary>
    public static class ControllerKindNames
    {
        /// <summary>Gets the localized accessible name for the illustration of the given kind.</summary>
        public static string GetAccessibleName(ControllerKind kind) => kind switch
        {
            ControllerKind.Xbox => Localizer.Get("ButtonsControllerImageName_Xbox"),
            ControllerKind.PlayStationDualShock4 => Localizer.Get("ButtonsControllerImageName_PlayStationDualShock4"),
            ControllerKind.PlayStationDualSense => Localizer.Get("ButtonsControllerImageName_PlayStationDualSense"),
            ControllerKind.SwitchPro => Localizer.Get("ButtonsControllerImageName_SwitchPro"),
            ControllerKind.JoyConLeft => Localizer.Get("ButtonsControllerImageName_JoyConLeft"),
            ControllerKind.JoyConRight => Localizer.Get("ButtonsControllerImageName_JoyConRight"),
            ControllerKind.JoyConPair => Localizer.Get("ButtonsControllerImageName_JoyConPair"),
            _ => Localizer.Get("ButtonsControllerImageName"),
        };
    }
}
