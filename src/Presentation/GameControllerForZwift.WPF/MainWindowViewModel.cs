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
        }

        // Command to be bound to the Button
        public ICommand ReadDataCommand { get; }

        // Method to execute when the button is clicked
        private void ReadData()
        {
            IInputService inputService = new GamepadService();
            DataIntegrator dataIntegrator = new DataIntegrator(inputService);

            var controllerData = dataIntegrator.ReadData();


            bool aIsPressed = (controllerData.Buttons & ControllerButtons.A) != ControllerButtons.None;
            ColorHex = aIsPressed ? Brushes.Black : Brushes.Red;
        }

        public ICommand RefreshListCommand { get; }

        // Method to execute when the button is clicked
        private void RefreshList()
        {
            IInputService inputService = new GamepadService();
            DataIntegrator dataIntegrator = new DataIntegrator(inputService);

            Controllers = inputService.GetControllers().ToList();
        }
    }
}
