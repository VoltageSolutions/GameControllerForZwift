using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public ControllerSetupViewModel()
        {
            _selectorViewModel = new ZwiftFunctionSelectorViewModel();
        }

        #endregion
    }
}
