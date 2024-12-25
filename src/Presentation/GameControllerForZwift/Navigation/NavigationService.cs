using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace GameControllerForZwift.UI.WPF.Navigation
{
    /// <summary>
    /// Service for navigating between pages.
    /// </summary>
    public class NavigationService : INavigationService
    {
        private Frame _frame;

        private Type _currentPageType = null;

        private readonly Stack<Type> _history;
        private readonly Stack<Type> _future;

        private readonly IServiceProvider _serviceProvider;

        public event EventHandler<NavigatingEventArgs> Navigating;


        public NavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _history = new Stack<Type>();
            _future = new Stack<Type>();
        }

        public Type CurrentPageType { get { return _currentPageType; } }
        public Stack<Type> History { get { return _history; } }
        public Stack<Type> Future { get { return _future; } }

        public void SetFrame(Frame frame)
        {
            _frame = frame;
        }

        // use this for navigating in ways that are specific to a feature-area that justifies clearing the future stack
        public void NavigateTo(Type type)
        {
            if (type != null)
            {
                _future.Clear();
                RaiseNavigatingEvent(type);
            }
        }

        // use this for major page changes with the left-hand navigation where going forward/back are both okay.
        public void Navigate(Type type)
        {
            if (type != null)
            {
                _history.Push(type);
                UpdateCurrentPage();
                var page = _serviceProvider.GetRequiredService(type);
                _frame.Navigate(page);
            }
        }

        public void UpdateCurrentPage()
        {
            // This will fail if the _history stack is empty.
            try
            {
                _currentPageType = _history.Peek();
            }
            catch (Exception ex) 
            {
                _currentPageType = null;
            }
        }

        public void NavigateBack()
        {
            if (_history.Count > 0)
            {
                // Put the current item in the future
                Type type = _history.Pop();
                _future.Push(type);

                // Navigate backwards
                UpdateCurrentPage();
                RaiseNavigatingEvent(_currentPageType);

                /*
                 In the current implementation, the following will happen:
                NavigateBack 
                    V
                RaiseNavigatingEvent 
                    V
                Window ControlsList Treeview attempts to select appropriate item to show to user
                    V
                ControlsList calls Navigate
                    V
                The item we are navigating to gets added to the history again.


                TODO - This behaves in weird ways if you use the Settings option. Refactor this.
                 */

                if (_history.Count > 0)
                {
                    _history.Pop();
                    UpdateCurrentPage();
                }
                
            }
        }

        public void NavigateForward()
        {
            if (_future.Count > 0)
            {
                Type type = _future.Pop();
                if (type != null)
                {
                    _history.Push(type);
                    RaiseNavigatingEvent(type);
                    _currentPageType = type;
                }
            }
        }

        public void RaiseNavigatingEvent(Type type)
        {
            Navigating?.Invoke(this, new NavigatingEventArgs(type));
        }

        public bool IsBackHistoryNonEmpty()
        {
            return _history.Count > 0;
        }
    }

}
