using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameControllerForZwift.Core;
using GameControllerForZwift.Logic;

namespace GameControllerForZwift.UI.WPF.ViewModels
{
    public partial class ControllerSetupViewModel : ObservableObject
    {
        #region Fields

        [ObservableProperty]
        private string _title = "Controller Setup";

        [ObservableProperty]
        private string _description = "Select a game controller and configure its button mapping to Zwift functions.";

        [ObservableProperty]
        ZwiftFunctionSelectorViewModel _selectorViewModel;

        private DataIntegrator _dataIntegrator;
        private IInputService _inputService;
        [ObservableProperty]
        private List<IController> _controllers;
        [ObservableProperty]
        private IController _selectedController;
        [ObservableProperty]
        private ControllerData _currentControllerValues;

        #endregion

        #region Constructor

        // todo - this should take an interface for the dataintegrator and should need need the input service
        public ControllerSetupViewModel(DataIntegrator dataIntegrator, IInputService inputService)
        {
            _dataIntegrator = dataIntegrator;
            _inputService = inputService;
            _selectorViewModel = new ZwiftFunctionSelectorViewModel();
        }

        #endregion

        #region Commands
        [RelayCommand]
        public void RefreshControllerList()
        {
            Controllers = _inputService.GetControllers().ToList();
        }

        [RelayCommand]
        public void ResetMappingsToDefault()
        {

        }

        [RelayCommand]
        public void ClearAllMappings()
        {
            
        }

        [RelayCommand]
        public void TestReadData()
        {
            if (null != SelectedController)
                CurrentControllerValues = _dataIntegrator.ReadData(SelectedController);

            //teset
            //CurrentControllerValues = new ControllerData { A = true };
            SelectorViewModel.ControllerData = CurrentControllerValues;

        }
        #endregion
    }
}
