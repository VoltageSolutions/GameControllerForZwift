using GameControllerForZwift.UI.WPF.ViewModels;
using System.Windows.Controls;

namespace GameControllerForZwift.UI.WPF.Views
{
    /// <summary>
    /// Interaction logic for AboutPage.xaml
    /// </summary>
    public partial class AboutPage : Page
    {
        #region Fields
        public AboutPageViewModel ViewModel { get; }
        #endregion
        #region Constructor
        public AboutPage(AboutPageViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
        #endregion
    }
}
