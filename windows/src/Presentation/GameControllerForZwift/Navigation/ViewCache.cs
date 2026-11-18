using System.Windows;

namespace GameControllerForZwift.UI.WPF.Navigation
{
    /// <summary>
    /// Creates the view for a page view model on first use and returns the same instance afterwards. The views share
    /// objects with their singleton view models (for example a FlowDocument, which can only have one parent), and
    /// reusing them keeps state such as scroll position.
    /// </summary>
    public sealed class ViewCache
    {
        private readonly Dictionary<Type, Func<FrameworkElement>> _factories;
        private readonly Dictionary<Type, FrameworkElement> _views = new();

        public ViewCache(IReadOnlyDictionary<Type, Func<FrameworkElement>> factories)
        {
            ArgumentNullException.ThrowIfNull(factories);
            _factories = new Dictionary<Type, Func<FrameworkElement>>(factories);
        }

        /// <summary>Returns the view for the view model, with the view model as its DataContext, or null when none is registered.</summary>
        public FrameworkElement? GetView(object? viewModel)
        {
            if (viewModel is null)
            {
                return null;
            }

            Type type = viewModel.GetType();
            if (_views.TryGetValue(type, out FrameworkElement? view))
            {
                return view;
            }

            if (!_factories.TryGetValue(type, out Func<FrameworkElement>? factory))
            {
                return null;
            }

            view = factory();
            view.DataContext = viewModel;
            _views[type] = view;
            return view;
        }
    }
}
