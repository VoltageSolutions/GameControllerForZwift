using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameControllerForZwift.Core;
using GameControllerForZwift.UI.WPF.Localization;
using System.ComponentModel;

namespace GameControllerForZwift.UI.WPF.ViewModels
{
    /// <summary>
    /// The side panel where you choose what the selected input does (SCR-BTN-032 to SCR-BTN-038). Choosing applies at once
    /// to the current mapping.
    /// </summary>
    public sealed partial class FunctionPickerViewModel : ObservableObject
    {
        private ButtonRowViewModel? _target;

        /// <summary>True while the panel is open.</summary>
        [ObservableProperty]
        private bool _isOpen;

        /// <summary>Builds the groups from <see cref="ButtonsCatalog.FunctionGroups"/>. A group with no function is left out.</summary>
        public FunctionPickerViewModel()
        {
            Groups = ButtonsCatalog.FunctionGroups
                .Where(g => g.Functions.Count > 0)
                .Select(g => new FunctionGroupViewModel(Localizer.Get(g.TitleKey),
                    g.Functions.Select(f => new FunctionItemViewModel(f, this)).ToList()))
                .ToList();
        }

        /// <summary>The groups, in the order the panel shows them.</summary>
        public IReadOnlyList<FunctionGroupViewModel> Groups { get; }

        /// <summary>The row the panel chooses a function for, or null while no row is selected.</summary>
        public ButtonRowViewModel? Target => _target;

        /// <summary>The panel's heading, for example "What should RB do?".</summary>
        public string Title => _target is null ? string.Empty : Localizer.Format("ButtonsPickerTitle", _target.Chip);

        /// <summary>Opens the panel for a row. A panel that is already open moves to the row.</summary>
        /// <param name="row">The row to choose a function for.</param>
        public void Open(ButtonRowViewModel row)
        {
            ArgumentNullException.ThrowIfNull(row);
            SetTarget(row);
            IsOpen = true;
        }

        /// <summary>Closes the panel.</summary>
        [RelayCommand]
        public void Close()
        {
            IsOpen = false;
            SetTarget(null);
        }

        /// <summary>Applies a function to the target row (SCR-BTN-038). The panel stays open.</summary>
        /// <param name="item">The chosen function.</param>
        public void Choose(FunctionItemViewModel item)
        {
            ArgumentNullException.ThrowIfNull(item);
            _target?.SetFunction(item.Function);
        }

        private void SetTarget(ButtonRowViewModel? row)
        {
            if (_target is not null)
            {
                _target.PropertyChanged -= OnTargetChanged;
            }

            _target = row;
            if (_target is not null)
            {
                _target.PropertyChanged += OnTargetChanged;
            }

            OnPropertyChanged(nameof(Target));
            OnPropertyChanged(nameof(Title));
            UpdateSelection();
        }

        private void OnTargetChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ButtonRowViewModel.Function))
            {
                UpdateSelection();
            }
        }

        private void UpdateSelection()
        {
            foreach (FunctionItemViewModel item in Groups.SelectMany(g => g.Items))
            {
                item.IsSelected = _target is not null && item.Function == _target.Function;
            }
        }
    }

    /// <summary>A group of functions in the picker.</summary>
    /// <param name="Title">The group's heading.</param>
    /// <param name="Items">The functions in the group.</param>
    public sealed record FunctionGroupViewModel(string Title, IReadOnlyList<FunctionItemViewModel> Items);

    /// <summary>One function in the picker, a radio choice (SCR-BTN-034).</summary>
    public sealed partial class FunctionItemViewModel : ObservableObject
    {
        private readonly FunctionPickerViewModel _picker;

        /// <summary>True while the function is the one mapped to the target row.</summary>
        [ObservableProperty]
        private bool _isSelected;

        /// <summary>Lists a function in a picker.</summary>
        /// <param name="info">The function's listing.</param>
        /// <param name="picker">The picker that applies the choice.</param>
        public FunctionItemViewModel(ButtonsFunctionInfo info, FunctionPickerViewModel picker)
        {
            ArgumentNullException.ThrowIfNull(info);
            ArgumentNullException.ThrowIfNull(picker);
            Function = info.Function;
            Title = Localizer.Get(info.TitleKey);
            Hint = Localizer.Get(info.HintKey);
            _picker = picker;
        }

        /// <summary>The function.</summary>
        public ZwiftFunction Function { get; }

        /// <summary>The automation ID, <c>FunctionItem_{function}</c>.</summary>
        public string AutomationId => "FunctionItem_" + Function;

        /// <summary>The function's name.</summary>
        public string Title { get; }

        /// <summary>One short line that says what the function does.</summary>
        public string Hint { get; }

        /// <summary>The name screen readers read, for example "Shift up, Harder gear".</summary>
        public string AccessibleName => Localizer.Format("FunctionItemName", Title, Hint);

        /// <summary>Chooses the function.</summary>
        [RelayCommand]
        private void Choose() => _picker.Choose(this);
    }
}
