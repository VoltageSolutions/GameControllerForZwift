using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace GameControllerForZwift.UI.WPF.Navigation
{
    /// <summary>
    /// Interface for the NavigationService
    /// </summary>
    public interface INavigationService
    {
        void Navigate(Type type);

        void NavigateTo(Type type);

        void SetFrame(Frame frame);
        void NavigateBack();

        void NavigateForward();

        bool IsBackHistoryNonEmpty();

        event EventHandler<NavigatingEventArgs> Navigating;
    }
}
