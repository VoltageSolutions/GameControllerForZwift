using GameControllerForZwift.Core;
using GameControllerForZwift.Logic;
using GameControllerForZwift.UI.WPF;
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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace GameControllerForZwift.UI.WPF.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        #region Fields
        //private readonly Dispatcher _dispatcher;
        private IInputService _inputService;
        private DataIntegrator _dataIntegrator;
        private List<IController> _controllers;
        private IController _selectedController;

        // test stuff
        
        private ControllerData _currentControllerValues;
        private List<ControllerData> _readValues;
        private ZwiftFunctionSelectorViewModel _firstFunctionVM;
        #endregion

        #region Constructor

        public MainWindowViewModel(DataIntegrator dataIntegrator, IInputService inputService)
        {
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
            ReadDataCommand = new RelayCommand(ReadData);
            RefreshListCommand = new RelayCommand(RefreshList);
            CheckQueueCommand = new RelayCommand(CheckQueue);
        }

        // Command to be bound to the Button
        public ICommand ReadDataCommand { get; private set; }

        // Method to execute when the button is clicked
        private async void ReadData()
        {
            //if (null != SelectedController)
            //    CurrentControllerValues = _dataIntegrator.ReadData(SelectedController);
            ////_dataIntegrator.StartProcessing();
            

            var cts = new CancellationTokenSource();
            //App.Current.Exit += (s, e) => cts.Cancel();
            var cancellationToken = cts.Token;


            while (!cancellationToken.IsCancellationRequested)
            {
                await ReadTheData(cancellationToken);
                await Task.Delay(50, cancellationToken);
            }
        }

        //temp async test stuff
        private async Task ReadTheData(CancellationToken cancellationToken)
        {
            if (null != SelectedController)
                CurrentControllerValues = _dataIntegrator.ReadData(SelectedController);
            //_dataIntegrator.StartProcessing();
        }


        public ICommand RefreshListCommand { get; private set; }

        // Method to execute when the button is clicked
        private void RefreshList()
        {
            Controllers = _inputService.GetControllers().ToList();
        }

        public ICommand CheckQueueCommand { get; private set; }

        // Method to execute when the button is clicked
        private void CheckQueue()
        {
            List<ControllerData> tempList = new List<ControllerData>();
            foreach (ControllerData data in _dataIntegrator.DataQueue)
                tempList.Add(data);

            ReadValues = tempList;
            //var count = _dataIntegrator.DataQueue.Count;

            //MessageBox.Show(string.Format("There are {0} entries in the queue", count.ToString()), "Check!");
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
