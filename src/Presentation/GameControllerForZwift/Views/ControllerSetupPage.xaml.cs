using GameControllerForZwift.UI.WPF.ViewModels;
using System.Windows.Controls;

namespace GameControllerForZwift.UI.WPF.Views
{
    /// <summary>
    /// Interaction logic for ControllerSetupPage.xaml
    /// </summary>
    public partial class ControllerSetupPage : UserControl
    {
        #region Fields

        /// <summary>The view model shown by this view. The view cache assigns it as the DataContext.</summary>
        public ControllerSetupViewModel ViewModel => (ControllerSetupViewModel)DataContext;

        #endregion

        #region Constructor

        public ControllerSetupPage()
        {
            InitializeComponent();
        }
        #endregion
    }
}
