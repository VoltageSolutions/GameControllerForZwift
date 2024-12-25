using GameControllerForZwift.UI.WPF.ViewModels;
using System.Diagnostics;
using System.Security.Policy;
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

        private void OpenHyperlink(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("cmd", $"/c start {e.Parameter.ToString()}") { CreateNoWindow = true });
            e.Handled = true;
        }
    }
}
