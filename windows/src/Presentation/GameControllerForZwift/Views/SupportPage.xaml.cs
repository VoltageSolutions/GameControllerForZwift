using GameControllerForZwift.UI.WPF.ViewModels;
using System.Windows.Controls;

namespace GameControllerForZwift.UI.WPF.Views
{
    /// <summary>The Support this project screen (docs/spec/screens/support.md).</summary>
    public partial class SupportPage : UserControl
    {
        /// <summary>The view model shown by this view. The view cache assigns it as the DataContext.</summary>
        public SupportViewModel ViewModel => (SupportViewModel)DataContext;

        public SupportPage()
        {
            InitializeComponent();
        }
    }
}
