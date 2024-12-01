using GameControllerForZwift.UI.WPF.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace GameControllerForZwift.UI.WPF.Views
{
    /// <summary>
    /// Interaction logic for ControllerSetupPage.xaml
    /// </summary>
    public partial class ControllerSetupPage : Page
    {
        #region Fields

        public ControllerSetupViewModel ViewModel { get; }

        #endregion

        #region Constructor

        public ControllerSetupPage(ControllerSetupViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
        #endregion
    }
}
