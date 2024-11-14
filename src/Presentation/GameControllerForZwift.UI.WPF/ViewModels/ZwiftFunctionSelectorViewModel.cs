using GameControllerForZwift.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameControllerForZwift.UI.WPF.ViewModels
{
    public class ZwiftFunctionSelectorViewModel : ViewModelBase
    {
        public ZwiftFunctionSelectorViewModel() { }

        public void EnablePlayerView()
        {
            ShowPlayerView = true;
            ShowRiderAction = false;
        }

        public void EnableRiderAction()
        {
            ShowPlayerView = false;
            ShowRiderAction = true;
        }

        public void HideSecondarySelections()
        {
            ShowPlayerView = false;
            ShowRiderAction = false;
        }

        public ZwiftFunction _selectedZwiftFunction;
        public ZwiftFunction SelectedZwiftFunction
        {
            get { return _selectedZwiftFunction; }
            set
            {
                _selectedZwiftFunction = value;
                NotifyPropertyChanged();

                if (ZwiftFunction.AdjustCameraAngle == _selectedZwiftFunction)
                    EnablePlayerView();
                else if (ZwiftFunction.RiderAction == _selectedZwiftFunction)
                    EnableRiderAction();
                else
                    HideSecondarySelections();
            }
        }

        public IEnumerable<ZwiftFunction> ZwiftFunctions
        {
            get
            {
                return Enum.GetValues(typeof(ZwiftFunction))
                    .Cast<ZwiftFunction>();
            }
        }

        private bool _showPlayerView;
        public bool ShowPlayerView
        {
            get { return _showPlayerView; }
            set { _showPlayerView = value; NotifyPropertyChanged(); }
        }

        public ZwiftPlayerView _selectedZwiftPlayerView;
        public ZwiftPlayerView SelectedZwiftPlayerView
        {
            get { return _selectedZwiftPlayerView; }
            set
            {
                _selectedZwiftPlayerView = value;
                NotifyPropertyChanged();
            }
        }

        public IEnumerable<ZwiftPlayerView> ZwiftPlayerViews
        {
            get
            {
                return Enum.GetValues(typeof(ZwiftPlayerView))
                    .Cast<ZwiftPlayerView>();
            }
        }

        private bool _showRiderAction;
        public bool ShowRiderAction
        {
            get { return _showRiderAction; }
            set { _showRiderAction = value; NotifyPropertyChanged(); }
        }

        public ZwiftRiderAction _selectedZwiftRiderAction;
        public ZwiftRiderAction SelectedZwiftRiderAction
        {
            get { return _selectedZwiftRiderAction; }
            set
            {
                _selectedZwiftRiderAction = value;
                NotifyPropertyChanged();
            }
        }

        public IEnumerable<ZwiftRiderAction> ZwiftRiderActions
        {
            get
            {
                return Enum.GetValues(typeof(ZwiftRiderAction))
                    .Cast<ZwiftRiderAction>();
            }
        }
    }
}
