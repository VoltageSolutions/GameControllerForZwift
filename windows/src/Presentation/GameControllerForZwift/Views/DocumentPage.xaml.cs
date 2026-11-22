using GameControllerForZwift.UI.WPF.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace GameControllerForZwift.UI.WPF.Views
{
    /// <summary>Shows a markdown document: the Credits list and the License.</summary>
    public partial class DocumentPage : UserControl
    {
        /// <summary>The view model shown by this view. The view cache assigns it as the DataContext.</summary>
        public DocumentPageViewModel ViewModel => (DocumentPageViewModel)DataContext;

        public DocumentPage()
        {
            InitializeComponent();
        }

        private void OpenHyperlink(object sender, ExecutedRoutedEventArgs e)
        {
            ViewModel.OpenLinkCommand.Execute(e.Parameter as string);
            e.Handled = true;
        }
    }
}
