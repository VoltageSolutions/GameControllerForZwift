using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameControllerForZwift.Core;
using GameControllerForZwift.Logic;
using System.Windows;

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


            _dataIntegrator.InputChanged += _dataIntegrator_InputChanged;
        }

        private void _dataIntegrator_InputChanged(object? sender, InputStateChangedEventArgs e)
        {
            // Ensure that UI updates happen on the UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                CurrentControllerValues = e.Data;
                SelectorViewModel.ControllerData = CurrentControllerValues;
            });
        }

        #endregion

        #region Commands
        [RelayCommand]
        public void RefreshControllerList()
        {
            Controllers = _dataIntegrator.GetControllers().ToList();
        }

        [RelayCommand]
        public void ResetMappingsToDefault()
        {

        }

        [RelayCommand]
        public void ClearAllMappings()
        {
            
        }

        
        #endregion

        #region Methods

        partial void OnSelectedControllerChanged(IController oldValue, IController newValue)
        {
            // Stop the previous processing, if any
            _dataIntegrator.StopProcessing();

            // Start processing the new controller if it's not null
            if (newValue != null)
            {
                _dataIntegrator.StartProcessing(newValue);
            }
        }

        #endregion
    }
}
