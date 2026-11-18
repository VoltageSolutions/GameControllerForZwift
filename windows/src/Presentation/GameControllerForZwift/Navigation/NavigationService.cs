using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace GameControllerForZwift.UI.WPF.Navigation
{
    /// <summary>
    /// Keeps the navigation history and the current view model. This is the single source of truth: the UI only
    /// displays <see cref="CurrentViewModel"/> and never keeps a journal of its own.
    /// </summary>
    /// <remarks>
    /// View models are resolved from the service provider. The main page view models are registered as singletons, so
    /// their state (for example the selected controller) is kept when the user comes back to the page.
    /// The history holds the previous pages only, never the current one.
    /// </remarks>
    public sealed class NavigationService : ObservableObject, INavigationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Stack<object> _history = new();
        private object? _currentViewModel;
        private bool _isNavigating;

        public NavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public object? CurrentViewModel => _currentViewModel;

        public bool CanGoBack => _history.Count > 0;

        public void NavigateTo<TViewModel>() where TViewModel : class
        {
            NavigateTo(typeof(TViewModel));
        }

        public void NavigateTo(Type viewModelType)
        {
            ArgumentNullException.ThrowIfNull(viewModelType);

            // A listener that reacts to a change by navigating again (for example a selection sync) must not add history.
            if (_isNavigating || _currentViewModel?.GetType() == viewModelType)
            {
                return;
            }

            object target = _serviceProvider.GetRequiredService(viewModelType);
            if (ReferenceEquals(target, _currentViewModel))
            {
                return;
            }

            if (_currentViewModel is not null)
            {
                _history.Push(_currentViewModel);
            }

            Show(target);
        }

        public void GoBack()
        {
            if (_isNavigating || _history.Count == 0)
            {
                return;
            }

            Show(_history.Pop());
        }

        private void Show(object viewModel)
        {
            _isNavigating = true;
            try
            {
                _currentViewModel = viewModel;
                OnPropertyChanged(nameof(CurrentViewModel));
                OnPropertyChanged(nameof(CanGoBack));
            }
            finally
            {
                _isNavigating = false;
            }
        }
    }
}
