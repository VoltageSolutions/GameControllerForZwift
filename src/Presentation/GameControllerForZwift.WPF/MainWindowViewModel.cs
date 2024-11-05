using GameControllerForZwift.Core;
using GameControllerForZwift.GamepadWinRT;
using GameControllerForZwift.Logic;
using GameControllerForZwift.UI.WPF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace GameControllerForZwift.WPF
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        //private readonly Dispatcher _dispatcher;
        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "") 
        { 
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); 
        }

        private ZwiftFunctionSelectorViewModel _firstFunctionVM;

        public ZwiftFunctionSelectorViewModel FirstFunctionVM
        {
            get { return _firstFunctionVM; }
            set { _firstFunctionVM = value; }
        }


        private Brush _colorHex = Brushes.Red;

        public Brush ColorHex
        {
            get { return _colorHex; }
            set { _colorHex = value; NotifyPropertyChanged(); }
        }

        private List<IController> _controllers;

        public List<IController> Controllers
        {
            get { return _controllers; }
            set { _controllers = value; NotifyPropertyChanged(); }
        }

        private IController _selectedController;

        public IController SelectedController
        {
            get { return _selectedController; }
            set { _selectedController = value; NotifyPropertyChanged(); }
        }


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
                    bool aIsPressed = (item.Buttons & ControllerButtons.A) != ControllerButtons.None;

                    //_dispatcher.Invoke(() =>
                    //{
                    //    ColorHex = aIsPressed ? Brushes.Black : Brushes.Red;
                    //});
                    
                    ColorHex = aIsPressed ? Brushes.Black : Brushes.Red;

                    await Task.Delay(50, cancellationToken);
                }
            }
        }

        public MainWindowViewModel()
        {
            //_dispatcher = Dispatcher.CurrentDispatcher;
            ColorHex = Brushes.Red;

            // todo - use ms DI container
            //IInputService inputService = new GamepadService();
            //DataIntegrator dataIntegrator = new DataIntegrator(inputService);

            //dataIntegrator.StartProcessing();

            // Token for graceful exit
            //var cts = new CancellationTokenSource();
            //App.Current.Exit += (s, e) => cts.Cancel();

            //dostuff(dataIntegrator, cts.Token);
            ReadDataCommand = new RelayCommand(ReadData);
            RefreshListCommand = new RelayCommand(RefreshList);
            CheckQueueCommand = new RelayCommand(CheckQueue);


            _inputService = new GamepadService();
            _dataIntegrator = new DataIntegrator(_inputService);

            FirstFunctionVM = new ZwiftFunctionSelectorViewModel();
        }

        private IInputService _inputService;
        private DataIntegrator _dataIntegrator;


        // Command to be bound to the Button
        public ICommand ReadDataCommand { get; }

        // Method to execute when the button is clicked
        private void ReadData()
        {
            var controllerData = _dataIntegrator.ReadData();


            bool aIsPressed = (controllerData.Buttons & ControllerButtons.A) != ControllerButtons.None;
            ColorHex = aIsPressed ? Brushes.Black : Brushes.Red;

            _dataIntegrator.StartProcessing();
        }

        public ICommand RefreshListCommand { get; }

        // Method to execute when the button is clicked
        private void RefreshList()
        {
            Controllers = _inputService.GetControllers().ToList();
        }

        public ICommand CheckQueueCommand { get; }

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

        private List<ControllerData> _readValues;

        public List<ControllerData> ReadValues
        {
            get { return _readValues; }
            set { _readValues = value; NotifyPropertyChanged(); }
        }

    }
}
