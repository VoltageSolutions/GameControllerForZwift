using GameControllerForZwift.UI.WPF.ViewModels;
using System.Windows.Controls;

namespace GameControllerForZwift.UI.WPF.Views
{
    /// <summary>The Credits screen (docs/spec/screens/credits.md).</summary>
    public partial class CreditsPage : UserControl
    {
        /// <summary>The view model shown by this view. The view cache assigns it as the DataContext.</summary>
        public CreditsViewModel ViewModel => (CreditsViewModel)DataContext;

        public CreditsPage()
        {
            InitializeComponent();
        }
    }
}
