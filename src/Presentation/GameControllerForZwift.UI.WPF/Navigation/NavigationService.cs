﻿using Microsoft.Extensions.DependencyInjection;
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

        public void SetFrame(Frame frame)
        {
            _frame = frame;
        }

        public void NavigateTo(Type type)
        {
            if (type != null)
            {
                _future.Clear();
                RaiseNavigatingEvent(type);
            }
        }

        public void Navigate(Type type)
        {
            if (type != null)
            {
                _history.Push(_currentPageType);
                _currentPageType = type;
                var page = _serviceProvider.GetRequiredService(type);
                _frame.Navigate(page);
            }
        }

        public void NavigateBack()
        {
            if (_history.Count > 0)
            {
                Type type = _history.Pop();
                if (type != null)
                {
                    _future.Push(type);
                    RaiseNavigatingEvent(type);
                    _history.Pop();
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
                }
            }
        }

        public void RaiseNavigatingEvent(Type type)
        {
            Navigating?.Invoke(this, new NavigatingEventArgs(type));
        }

        public bool IsBackHistoryNonEmpty()
        {
            var item = _history.Peek();
            if (item == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }

}
