using CommunityToolkit.Mvvm.ComponentModel;
using GameControllerForZwift.Core;

namespace GameControllerForZwift.UI.WPF.ViewModels
{
    public partial class ZwiftFunctionSelectorViewModel : ObservableObject
    {
        #region Fields

        [ObservableProperty]
        private ZwiftFunction _selectedZwiftFunction;

        partial void OnSelectedZwiftFunctionChanged(ZwiftFunction value)
        {
            if (ZwiftFunction.AdjustCameraAngle == value)
                EnablePlayerView();
            else if (ZwiftFunction.RiderAction == value)
                EnableRiderAction();
            else
                HideSecondarySelections();
        }

        public IEnumerable<ZwiftFunction> ZwiftFunctions
        {
            get
            {
                return Enum.GetValues(typeof(ZwiftFunction))
                    .Cast<ZwiftFunction>();
            }
        }

        [ObservableProperty]
        private bool _showPlayerView;


        [ObservableProperty]
        private ZwiftPlayerView _selectedZwiftPlayerView;


        public IEnumerable<ZwiftPlayerView> ZwiftPlayerViews
        {
            get
            {
                return Enum.GetValues(typeof(ZwiftPlayerView))
                    .Cast<ZwiftPlayerView>();
            }
        }

        [ObservableProperty]
        private bool _showRiderAction;


        [ObservableProperty]
        private ZwiftRiderAction _selectedZwiftRiderAction;


        public IEnumerable<ZwiftRiderAction> ZwiftRiderActions
        {
            get
            {
                return Enum.GetValues(typeof(ZwiftRiderAction))
                    .Cast<ZwiftRiderAction>();
            }
        }

        // test
        [ObservableProperty]
        private ControllerData _controllerData;

        #endregion

        #region Methods
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
        #endregion
    }
}
