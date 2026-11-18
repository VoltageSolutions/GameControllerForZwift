using GameControllerForZwift.Core.Controller;
using CommunityToolkit.Mvvm.ComponentModel;
using GameControllerForZwift.Core;
using GameControllerForZwift.Core.Mapping;

namespace GameControllerForZwift.UI.WPF.ViewModels
{
    public partial class ZwiftFunctionSelectorViewModel : ObservableObject
    {
        #region Properties

        [ObservableProperty]
        private string _inputName = string.Empty;

        /*
         * ControllerData includes the current button-press states.
         * SelectedInput is the input this ViewModel will use.
         * IsPressed highlights the View to show the status
         */

        [ObservableProperty]
        private ControllerData _controllerData = new();

        [ObservableProperty]
        private ControllerInput _selectedInput;

        [ObservableProperty]
        private ZwiftFunction _selectedZwiftFunction;

        public IEnumerable<ZwiftFunction> ZwiftFunctions { get; } = GetEnumValues<ZwiftFunction>();

        public event EventHandler<EventArgs>? MappingChanged;

        #endregion

        #region Constructor

        public ZwiftFunctionSelectorViewModel()
        {
            ControllerData = new ControllerData();
        }

        #endregion

        #region Methods

        public bool IsPressed => ControllerData.IsPressed(SelectedInput);

        partial void OnControllerDataChanged(ControllerData? oldValue, ControllerData newValue)
        {
            OnPropertyChanged(nameof(IsPressed));
        }

        public static IEnumerable<T> GetEnumValues<T>() where T : struct, Enum
        {
            return Enum.GetValues<T>();
        }

        partial void OnSelectedZwiftFunctionChanged(ZwiftFunction value)
        {
            MappingChanged?.Invoke(this, new EventArgs());
        }

        protected virtual void OnMappingChanged(EventArgs e)
        {
            MappingChanged?.Invoke(this, e);
        }

        public void SetInputMapping(InputMapping inputMap)
        {
            SelectedZwiftFunction = inputMap.Function;
        }

        public InputMapping GetInputMapping()
        {
            return new InputMapping(SelectedInput, SelectedZwiftFunction);
        }

        #endregion
    }
}
