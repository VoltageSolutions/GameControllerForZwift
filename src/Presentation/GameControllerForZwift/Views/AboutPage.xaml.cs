using GameControllerForZwift.UI.WPF.ViewModels;
using System.Diagnostics;
using System.Security.Policy;
using System.Windows.Controls;

namespace GameControllerForZwift.UI.WPF.Views
{
    /// <summary>
    /// Interaction logic for AboutPage.xaml
    /// </summary>
    public partial class AboutPage : UserControl
    {
        #region Fields

        /// <summary>The view model shown by this view. The data template assigns it as the DataContext.</summary>
        public AboutPageViewModel ViewModel => (AboutPageViewModel)DataContext;

        #endregion

        #region Constructor

        public AboutPage()
        {
            InitializeComponent();
        }
        #endregion

        private void OpenHyperlink(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("cmd", $"/c start {e.Parameter}") { CreateNoWindow = true });
            e.Handled = true;
        }
    }
}
