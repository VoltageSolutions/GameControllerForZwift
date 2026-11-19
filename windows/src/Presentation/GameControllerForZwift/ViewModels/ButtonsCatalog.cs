using GameControllerForZwift.Core;
using GameControllerForZwift.Core.Controller;

namespace GameControllerForZwift.UI.WPF.ViewModels
{
    /// <summary>Which tab of the Buttons screen lists an input (SCR-BTN-001).</summary>
    public enum ButtonsTab
    {
        /// <summary>Buttons, the D-pad, and the bumpers.</summary>
        Buttons,

        /// <summary>Sticks, stick clicks, and triggers.</summary>
        SticksAndTriggers,
    }

    /// <summary>A controller input as the Buttons screen lists it.</summary>
    /// <param name="Input">The input.</param>
    /// <param name="Key">The key in the row's automation ID, <c>ButtonsRow_{Key}</c> (SCR-BTN accessibility IDs).</param>
    /// <param name="ChipKey">The string key of the short label.</param>
    /// <param name="NameKey">The string key of the spoken name.</param>
    /// <param name="Tab">The tab that lists the input.</param>
    public sealed record ButtonsInputInfo(ControllerInput Input, string Key, string ChipKey, string NameKey, ButtonsTab Tab);

    /// <summary>A Zwift function as the function picker lists it.</summary>
    /// <param name="Function">The function.</param>
    /// <param name="TitleKey">The string key of the function's name.</param>
    /// <param name="HintKey">The string key of the line under the name.</param>
    public sealed record ButtonsFunctionInfo(ZwiftFunction Function, string TitleKey, string HintKey);

    /// <summary>A group of functions in the picker (SCR-BTN-032).</summary>
    /// <param name="TitleKey">The string key of the group's heading.</param>
    /// <param name="Functions">The functions in the group.</param>
    public sealed record ButtonsFunctionGroupInfo(string TitleKey, IReadOnlyList<ButtonsFunctionInfo> Functions);

    /// <summary>
    /// The inputs the Buttons screen lists and the functions it offers, with the string keys of their names
    /// (docs/spec/screens/buttons.md).
    /// </summary>
    public static class ButtonsCatalog
    {
        /// <summary>Every input, in the order the screen lists them.</summary>
        public static IReadOnlyList<ButtonsInputInfo> Inputs { get; } =
        [
            new(ControllerInput.A, "A", "ButtonsInputAChip", "ButtonsInputAName", ButtonsTab.Buttons),
            new(ControllerInput.B, "B", "ButtonsInputBChip", "ButtonsInputBName", ButtonsTab.Buttons),
            new(ControllerInput.X, "X", "ButtonsInputXChip", "ButtonsInputXName", ButtonsTab.Buttons),
            new(ControllerInput.Y, "Y", "ButtonsInputYChip", "ButtonsInputYName", ButtonsTab.Buttons),
            new(ControllerInput.Menu, "Menu", "ButtonsInputMenuChip", "ButtonsInputMenuName", ButtonsTab.Buttons),
            new(ControllerInput.View, "View", "ButtonsInputViewChip", "ButtonsInputViewName", ButtonsTab.Buttons),
            new(ControllerInput.DPadUp, "DPadUp", "ButtonsInputDPadUpChip", "ButtonsInputDPadUpName", ButtonsTab.Buttons),
            new(ControllerInput.DPadDown, "DPadDown", "ButtonsInputDPadDownChip", "ButtonsInputDPadDownName", ButtonsTab.Buttons),
            new(ControllerInput.DPadLeft, "DPadLeft", "ButtonsInputDPadLeftChip", "ButtonsInputDPadLeftName", ButtonsTab.Buttons),
            new(ControllerInput.DPadRight, "DPadRight", "ButtonsInputDPadRightChip", "ButtonsInputDPadRightName", ButtonsTab.Buttons),
            new(ControllerInput.LeftBumper, "LB", "ButtonsInputLeftBumperChip", "ButtonsInputLeftBumperName", ButtonsTab.Buttons),
            new(ControllerInput.RightBumper, "RB", "ButtonsInputRightBumperChip", "ButtonsInputRightBumperName", ButtonsTab.Buttons),
            new(ControllerInput.LeftThumbstickClick, "LS", "ButtonsInputLeftThumbstickClickChip", "ButtonsInputLeftThumbstickClickName", ButtonsTab.SticksAndTriggers),
            new(ControllerInput.LeftThumbstickTiltUp, "LSUp", "ButtonsInputLeftThumbstickTiltUpChip", "ButtonsInputLeftThumbstickTiltUpName", ButtonsTab.SticksAndTriggers),
            new(ControllerInput.LeftThumbstickTiltDown, "LSDown", "ButtonsInputLeftThumbstickTiltDownChip", "ButtonsInputLeftThumbstickTiltDownName", ButtonsTab.SticksAndTriggers),
            new(ControllerInput.LeftThumbstickTiltLeft, "LSLeft", "ButtonsInputLeftThumbstickTiltLeftChip", "ButtonsInputLeftThumbstickTiltLeftName", ButtonsTab.SticksAndTriggers),
            new(ControllerInput.LeftThumbstickTiltRight, "LSRight", "ButtonsInputLeftThumbstickTiltRightChip", "ButtonsInputLeftThumbstickTiltRightName", ButtonsTab.SticksAndTriggers),
            new(ControllerInput.RightThumbstickClick, "RS", "ButtonsInputRightThumbstickClickChip", "ButtonsInputRightThumbstickClickName", ButtonsTab.SticksAndTriggers),
            new(ControllerInput.RightThumbstickTiltUp, "RSUp", "ButtonsInputRightThumbstickTiltUpChip", "ButtonsInputRightThumbstickTiltUpName", ButtonsTab.SticksAndTriggers),
            new(ControllerInput.RightThumbstickTiltDown, "RSDown", "ButtonsInputRightThumbstickTiltDownChip", "ButtonsInputRightThumbstickTiltDownName", ButtonsTab.SticksAndTriggers),
            new(ControllerInput.RightThumbstickTiltLeft, "RSLeft", "ButtonsInputRightThumbstickTiltLeftChip", "ButtonsInputRightThumbstickTiltLeftName", ButtonsTab.SticksAndTriggers),
            new(ControllerInput.RightThumbstickTiltRight, "RSRight", "ButtonsInputRightThumbstickTiltRightChip", "ButtonsInputRightThumbstickTiltRightName", ButtonsTab.SticksAndTriggers),
            new(ControllerInput.LeftTrigger, "LT", "ButtonsInputLeftTriggerChip", "ButtonsInputLeftTriggerName", ButtonsTab.SticksAndTriggers),
            new(ControllerInput.RightTrigger, "RT", "ButtonsInputRightTriggerChip", "ButtonsInputRightTriggerName", ButtonsTab.SticksAndTriggers),
        ];

        /// <summary>
        /// The function groups in the order the picker shows them (SCR-BTN-032). The This app group is empty because the app
        /// implements no function of its own, so the picker leaves it out (SCR-BTN-036).
        /// </summary>
        public static IReadOnlyList<ButtonsFunctionGroupInfo> FunctionGroups { get; } =
        [
            new("FunctionGroupRiding",
                [
                    new(ZwiftFunction.ShiftUp, "FunctionShiftUpTitle", "FunctionShiftUpHint"),
                    new(ZwiftFunction.ShiftDown, "FunctionShiftDownTitle", "FunctionShiftDownHint"),
                    new(ZwiftFunction.SteerLeft, "FunctionSteerLeftTitle", "FunctionSteerLeftHint"),
                    new(ZwiftFunction.SteerRight, "FunctionSteerRightTitle", "FunctionSteerRightHint"),
                    new(ZwiftFunction.Brake, "FunctionBrakeTitle", "FunctionBrakeHint"),
                ]),
            new("FunctionGroupZwift",
                [
                    new(ZwiftFunction.RideOn, "FunctionRideOnTitle", "FunctionRideOnHint"),
                    new(ZwiftFunction.Powerup, "FunctionPowerupTitle", "FunctionPowerupHint"),
                ]),
            new("FunctionGroupMenus",
                [
                    new(ZwiftFunction.NavigateUp, "FunctionNavigateUpTitle", "FunctionNavigateUpHint"),
                    new(ZwiftFunction.NavigateDown, "FunctionNavigateDownTitle", "FunctionNavigateDownHint"),
                    new(ZwiftFunction.NavigateLeft, "FunctionNavigateLeftTitle", "FunctionNavigateLeftHint"),
                    new(ZwiftFunction.NavigateRight, "FunctionNavigateRightTitle", "FunctionNavigateRightHint"),
                    new(ZwiftFunction.Select, "FunctionSelectTitle", "FunctionSelectHint"),
                    new(ZwiftFunction.GoBack, "FunctionGoBackTitle", "FunctionGoBackHint"),
                ]),
            new("FunctionGroupApp",
                [
                ]),
            new("FunctionGroupNothing",
                [
                    new(ZwiftFunction.None, "FunctionNoneTitle", "FunctionNoneHint"),
                ]),
        ];

        private static readonly Dictionary<ZwiftFunction, ButtonsFunctionInfo> FunctionsByValue =
            FunctionGroups.SelectMany(g => g.Functions).ToDictionary(f => f.Function);

        private static readonly Dictionary<ControllerInput, ButtonsInputInfo> InputsByValue =
            Inputs.ToDictionary(i => i.Input);

        /// <summary>Returns the listing of a function. Every <see cref="ZwiftFunction"/> is listed.</summary>
        public static ButtonsFunctionInfo Function(ZwiftFunction function) => FunctionsByValue[function];

        /// <summary>Returns the listing of an input. Every <see cref="ControllerInput"/> is listed.</summary>
        public static ButtonsInputInfo Input(ControllerInput input) => InputsByValue[input];

        /// <summary>
        /// Returns the first input, in listing order, that is pressed in <paramref name="current"/> and was not pressed in
        /// <paramref name="previous"/>, or null when no press started.
        /// </summary>
        /// <param name="previous">The state before, or null when there is none.</param>
        /// <param name="current">The state now.</param>
        public static ControllerInput? FirstNewPress(ControllerData? previous, ControllerData current)
        {
            ArgumentNullException.ThrowIfNull(current);
            foreach (ButtonsInputInfo info in Inputs)
            {
                if (current.IsPressed(info.Input) && !(previous?.IsPressed(info.Input) ?? false))
                {
                    return info.Input;
                }
            }

            return null;
        }
    }
}
