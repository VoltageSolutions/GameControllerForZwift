using CommunityToolkit.Mvvm.ComponentModel;
using GameControllerForZwift.Core;
using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.UI.WPF.Localization;
using System.ComponentModel;

namespace GameControllerForZwift.UI.WPF.ViewModels
{
    /// <summary>
    /// One row of the Buttons list: an input and what it does, for example "Right bumper: Shift up" (SCR-BTN-003).
    /// </summary>
    public sealed partial class ButtonRowViewModel : ObservableObject
    {
        private const double TriggerAxisMax = 32767.0;

        private readonly ZwiftFunctionSelectorViewModel _selector;

        /// <summary>True while the row is the selected one (SCR-BTN-005).</summary>
        [ObservableProperty]
        private bool _isSelected;

        /// <summary>True when what the input does differs from the selected mapping as saved.</summary>
        [ObservableProperty]
        private bool _isChanged;

        /// <summary>Lists an input and follows the function mapped to it.</summary>
        /// <param name="info">The input's listing.</param>
        /// <param name="selector">Holds the function mapped to the input and the live controller state.</param>
        public ButtonRowViewModel(ButtonsInputInfo info, ZwiftFunctionSelectorViewModel selector)
        {
            ArgumentNullException.ThrowIfNull(info);
            ArgumentNullException.ThrowIfNull(selector);
            Info = info;
            _selector = selector;
            selector.PropertyChanged += OnSelectorChanged;
        }

        /// <summary>The input's listing.</summary>
        public ButtonsInputInfo Info { get; }

        /// <summary>The input.</summary>
        public ControllerInput Input => Info.Input;

        /// <summary>The row's automation ID, <c>ButtonsRow_{input}</c>.</summary>
        public string AutomationId => "ButtonsRow_" + Info.Key;

        /// <summary>The short label, for example RB.</summary>
        public string Chip => Localizer.Get(Info.ChipKey);

        /// <summary>The spoken name, for example Right bumper.</summary>
        public string InputName => Localizer.Get(Info.NameKey);

        /// <summary>The function mapped to the input.</summary>
        public ZwiftFunction Function => _selector.SelectedZwiftFunction;

        /// <summary>The function's name, or "No action" when none is mapped (SCR-BTN-003).</summary>
        public string FunctionTitle => Localizer.Get(ButtonsCatalog.Function(Function).TitleKey);

        /// <summary>True when no function is mapped, so the input is free (SCR-BTN-004).</summary>
        public bool IsFree => Function == ZwiftFunction.None;

        /// <summary>The name screen readers read, for example "Right bumper: Shift up".</summary>
        public string AccessibleName => Localizer.Format("ButtonsRowName", InputName, FunctionTitle);

        /// <summary>True while the input is pressed on the controller.</summary>
        public bool IsPressed => _selector.IsPressed;

        /// <summary>True for the two analog triggers, whose row fills as the trigger is pulled (SCR-BTN-041).</summary>
        public bool IsAnalogTrigger => Info.Input is ControllerInput.LeftTrigger or ControllerInput.RightTrigger;

        /// <summary>How far the trigger is pulled, 0 to 1, or 0 for any other input.</summary>
        public double PullRatio => Info.Input switch
        {
            ControllerInput.LeftTrigger => Math.Clamp(_selector.ControllerData.LeftTrigger / TriggerAxisMax, 0.0, 1.0),
            ControllerInput.RightTrigger => Math.Clamp(_selector.ControllerData.RightTrigger / TriggerAxisMax, 0.0, 1.0),
            _ => 0.0,
        };

        /// <summary>The live controller state, for the stick and trigger drawings.</summary>
        public ControllerData ControllerData => _selector.ControllerData;

        /// <summary>Sets the function mapped to the input.</summary>
        /// <param name="function">The function.</param>
        public void SetFunction(ZwiftFunction function) => _selector.SelectedZwiftFunction = function;

        private void OnSelectorChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ZwiftFunctionSelectorViewModel.SelectedZwiftFunction):
                    OnPropertyChanged(nameof(Function));
                    OnPropertyChanged(nameof(FunctionTitle));
                    OnPropertyChanged(nameof(IsFree));
                    OnPropertyChanged(nameof(AccessibleName));
                    break;
                case nameof(ZwiftFunctionSelectorViewModel.IsPressed):
                    OnPropertyChanged(nameof(IsPressed));
                    OnPropertyChanged(nameof(PullRatio));
                    break;
            }
        }
    }
}
