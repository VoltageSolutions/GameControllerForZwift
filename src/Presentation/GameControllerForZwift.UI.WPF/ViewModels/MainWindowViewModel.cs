using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameControllerForZwift.Core;
using GameControllerForZwift.Keyboard;
using GameControllerForZwift.Logic;
using GameControllerForZwift.UI.WPF;
using GameControllerForZwift.UI.WPF.Models;
using GameControllerForZwift.UI.WPF.Navigation;
using GameControllerForZwift.UI.WPF.Views;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace GameControllerForZwift.UI.WPF.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject //ViewModelBase
    {
        #region Fields
        //private readonly Dispatcher _dispatcher;
        private IInputService _inputService;
        private DataIntegrator _dataIntegrator;
        private List<IController> _controllers;
        private IController _selectedController;

        [ObservableProperty]
        private ICollection<ControlInfoDataItem> _controls;
        [ObservableProperty]
        private ControlInfoDataItem? _selectedControl;
        private readonly INavigationService _navigationService;
        [ObservableProperty]
        private bool _canNavigateback;

        private readonly ILogger<MainWindowViewModel> _logger;

        // test stuff

        private ControllerData _currentControllerValues;
        private List<ControllerData> _readValues;
        private ZwiftFunctionSelectorViewModel _firstFunctionVM;

        
        #endregion

        #region Constructor

        public MainWindowViewModel(INavigationService navigationService, ILogger<MainWindowViewModel> logger, DataIntegrator dataIntegrator, IInputService inputService)
        {
            _controls = ControlsInfoDataSource.Instance.ControlsInfo;
            _navigationService = navigationService;
            _logger = logger;
            //_dispatcher = Dispatcher.CurrentDispatcher;
            _inputService = inputService;
            _dataIntegrator = dataIntegrator;


            //IInputService inputService = new GamepadService();
            //DataIntegrator dataIntegrator = new DataIntegrator(inputService);

            //dataIntegrator.StartProcessing();

            // Token for graceful exit
            //var cts = new CancellationTokenSource();
            //App.Current.Exit += (s, e) => cts.Cancel();

            //dostuff(dataIntegrator, cts.Token);
            InitializeCommands();
            


            //_inputService = new GamepadService();
            

            FirstFunctionVM = new ZwiftFunctionSelectorViewModel();
            CurrentControllerValues = new ControllerData();
        }

        #endregion

        #region Properties

        public List<IController> Controllers
        {
            get => _controllers;
            set => SetProperty(ref _controllers, value);
        }
        public IController SelectedController
        {
            get => _selectedController;
            set => SetProperty(ref _selectedController, value);
        }

        public ZwiftFunctionSelectorViewModel FirstFunctionVM
        {
            get => _firstFunctionVM;
            set => SetProperty(ref _firstFunctionVM, value);
        }

        public ControllerData CurrentControllerValues
        {
            get => _currentControllerValues;
            set => SetProperty(ref _currentControllerValues, value);
        }

        public List<ControllerData> ReadValues
        {
            get => _readValues;
            set => SetProperty(ref _readValues, value);
        }

        #endregion

        #region Commands

        private void InitializeCommands()
        {
            //ReadDataCommand = new RelayCommand(ReadData);
            //RefreshListCommand = new RelayCommand(RefreshList);
            //CheckQueueCommand = new RelayCommand(CheckQueue);
        }

        // Command to be bound to the Button
        //public ICommand ReadDataCommand { get; private set; }

        // Method to execute when the button is clicked
        [RelayCommand]
        public async void ReadData()
        {
            _logger.LogInformation("Starting to read data.");
            //if (null != SelectedController)
            //    CurrentControllerValues = _dataIntegrator.ReadData(SelectedController);
            ////_dataIntegrator.StartProcessing();
            

            var cts = new CancellationTokenSource();
            //App.Current.Exit += (s, e) => cts.Cancel();
            var cancellationToken = cts.Token;


            while (!cancellationToken.IsCancellationRequested)
            {
                await ReadTheData(cancellationToken);
                await Task.Delay(150, cancellationToken);
            }
        }

        // this should be an interface and it should return something back
        private KeyboardNavigator _keySimulator = new KeyboardNavigator();

        //temp async test stuff
        private async Task ReadTheData(CancellationToken cancellationToken)
        {
            if (null != SelectedController)
                CurrentControllerValues = _dataIntegrator.ReadData(SelectedController);
            //_dataIntegrator.StartProcessing();

            if (CurrentControllerValues.A)
                _keySimulator.PerformAction(ZwiftFunction.Select);
            if (CurrentControllerValues.B)
                _keySimulator.PerformAction(ZwiftFunction.GoBack);
            if (CurrentControllerValues.Y)
                _keySimulator.PerformAction(ZwiftFunction.HideHUD);
            if (CurrentControllerValues.DPadUp)
                _keySimulator.PerformAction(ZwiftFunction.ShowMenu);
            if (CurrentControllerValues.DPadDown)
                _keySimulator.PerformAction(ZwiftFunction.Uturn);
            if (CurrentControllerValues.DPadLeft)
                _keySimulator.PerformAction(ZwiftFunction.NavigateLeft);
            if (CurrentControllerValues.DPadRight)
                _keySimulator.PerformAction(ZwiftFunction.NavigateRight);
            if (CurrentControllerValues.LeftBumper)
                _keySimulator.PerformAction(ZwiftFunction.FTPBiasDown);
            if (CurrentControllerValues.RightBumper)
                _keySimulator.PerformAction(ZwiftFunction.FTPBiasUp);
        }


        //public ICommand RefreshListCommand { get; private set; }

        // Method to execute when the button is clicked
        [RelayCommand]
        public void RefreshList()
        {
            Controllers = _inputService.GetControllers().ToList();
        }

        //public ICommand CheckQueueCommand { get; private set; }

        // Method to execute when the button is clicked
        [RelayCommand]
        public void CheckQueue()
        {
            List<ControllerData> tempList = new List<ControllerData>();
            foreach (ControllerData data in _dataIntegrator.DataQueue)
                tempList.Add(data);

            ReadValues = tempList;
            //var count = _dataIntegrator.DataQueue.Count;

            //MessageBox.Show(string.Format("There are {0} entries in the queue", count.ToString()), "Check!");
        }

        [RelayCommand]
        public void Settings()
        {
            _navigationService.Navigate(typeof(SettingsPage));
        }

        [RelayCommand]
        public void Back()
        {
            _navigationService.NavigateBack();
        }

        [RelayCommand]
        public void Forward()
        {
            _navigationService.NavigateForward();
        }

        public List<ControlInfoDataItem> GetNavigationItemHierarchyFromPageType(Type? pageType)
        {
            List<ControlInfoDataItem> list = new List<ControlInfoDataItem>();
            Stack<ControlInfoDataItem> _stack = new Stack<ControlInfoDataItem>();
            Stack<ControlInfoDataItem> _revStack = new Stack<ControlInfoDataItem>();

            if (pageType == null)
            {
                return list;
            }
            bool found = false;

            foreach (var item in Controls)
            {
                _stack.Push(item);
                found = FindNavigationItemsHierarchyFromPageType(pageType, item.Items, ref _stack);
                if (found)
                {
                    break;
                }
                _stack.Pop();
            }

            while (_stack.Count > 0)
            {
                _revStack.Push(_stack.Pop());
            }

            foreach (var item in _revStack)
            {
                list.Add(item);
            }

            return list;
        }

        private bool FindNavigationItemsHierarchyFromPageType(Type pageType, ICollection<ControlInfoDataItem> pages, ref Stack<ControlInfoDataItem> stack)
        {
            var item = stack.Peek();
            bool found = false;

            if (pageType == item.PageType)
            {
                return true;
            }

            foreach (var child in item.Items)
            {
                stack.Push(child);
                found = FindNavigationItemsHierarchyFromPageType(pageType, child.Items, ref stack);
                if (found) { return true; }
                stack.Pop();
            }

            return false;
        }

        public void UpdateCanNavigateBack()
        {
            CanNavigateback = _navigationService.IsBackHistoryNonEmpty();
        }

        #endregion

        #region Methods


        //public async void dostuff(DataIntegrator dataIntegrator, CancellationToken cancellationToken)
        //{
        //    // Periodically check the queue and display any new data
        //    //await Task.Run(() =>MonitorQueueAsync(dataIntegrator, cancellationToken));
        //    //await MonitorQueueAsync(dataIntegrator, cancellationToken);
        //    MonitorQueueAsync(dataIntegrator, cancellationToken);
        //}

        async Task MonitorQueueAsync(DataIntegrator dataProcessor, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (var item in dataProcessor.DataQueue)
                {
                    // do stuff on screen here

                    await Task.Delay(50, cancellationToken);
                }
            }
        }
        #endregion
    }
}
