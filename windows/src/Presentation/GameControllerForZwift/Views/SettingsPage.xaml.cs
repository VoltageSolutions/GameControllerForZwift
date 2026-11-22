using GameControllerForZwift.UI.WPF.ViewModels;
using System.Windows.Controls;

namespace GameControllerForZwift.UI.WPF.Views
{
    /// <summary>The Settings screen (docs/spec/screens/settings.md).</summary>
    public partial class SettingsPage : UserControl
    {
        /// <summary>The view model shown by this view. The view cache assigns it as the DataContext.</summary>
        public SettingsViewModel ViewModel => (SettingsViewModel)DataContext;

        public SettingsPage()
        {
            InitializeComponent();
        }
    }
}
