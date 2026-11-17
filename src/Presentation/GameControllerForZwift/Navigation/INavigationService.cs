using System.ComponentModel;

namespace GameControllerForZwift.UI.WPF.Navigation
{
    /// <summary>
    /// Owns the one and only navigation history of the app. It works with view models only (no WPF types). The
    /// shell shows the view that matches <see cref="CurrentViewModel"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="INotifyPropertyChanged.PropertyChanged"/> is raised for <see cref="CurrentViewModel"/> and
    /// <see cref="CanGoBack"/>.
    /// </remarks>
    public interface INavigationService : INotifyPropertyChanged
    {
        /// <summary>The view model of the page being shown, or null before the first navigation.</summary>
        object? CurrentViewModel { get; }

        /// <summary>True when there is a previous page to return to.</summary>
        bool CanGoBack { get; }

        /// <summary>
        /// Shows the view model of the given type. The previous page is pushed onto the history. Navigating to the page
        /// that is already shown, or navigating while another navigation is still being announced, does nothing.
        /// </summary>
        void NavigateTo(Type viewModelType);

        /// <summary>Shows the view model of type <typeparamref name="TViewModel"/>. See <see cref="NavigateTo(Type)"/>.</summary>
        void NavigateTo<TViewModel>() where TViewModel : class;

        /// <summary>Returns to the previous page. Does nothing when the history is empty.</summary>
        void GoBack();
    }
}
