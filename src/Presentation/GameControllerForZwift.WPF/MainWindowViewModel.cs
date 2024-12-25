using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameControllerForZwift.UI.WPF.Navigation;
using GameControllerForZwift.UI.WPF.Views;
using Microsoft.Extensions.Logging;

namespace GameControllerForZwift.UI.WPF
{
    public partial class MainWindowViewModel : ObservableObject
    {
        #region Fields

        [ObservableProperty]
        private string _applicationTitle = "Game Controller For Zwift";

        // Facilitate Navigation
        [ObservableProperty]
        private ICollection<PagesInfoDataItem> _pages;
        [ObservableProperty]
        private PagesInfoDataItem? _selectedControl;
        private readonly INavigationService _navigationService;
        [ObservableProperty]
        private bool _canNavigateback;

        // The following may need to go to other VMs instead
        private readonly ILogger<MainWindowViewModel> _logger;
        #endregion

        #region Constructor

        public MainWindowViewModel(INavigationService navigationService, ILogger<MainWindowViewModel> logger)
        {
            // Setup Navigation
            _pages = PagesInfoDataSource.Instance.PagesInfo;
            _navigationService = navigationService;

            // Other DI
            _logger = logger;
        }

        #endregion

        #region Commands
        [RelayCommand]
        public void Settings()
        {
            _navigationService.Navigate(typeof(SettingsPage));
        }

        [RelayCommand]
        public void Back()
        {
            _navigationService.NavigateBack();
        }

        [RelayCommand]
        public void Forward()
        {
            _navigationService.NavigateForward();
        }

        public List<PagesInfoDataItem> GetNavigationItemHierarchyFromPageType(Type? pageType)
        {
            List<PagesInfoDataItem> list = new List<PagesInfoDataItem>();
            Stack<PagesInfoDataItem> _stack = new Stack<PagesInfoDataItem>();
            Stack<PagesInfoDataItem> _revStack = new Stack<PagesInfoDataItem>();

            if (pageType == null)
            {
                return list;
            }
            bool found = false;

            foreach (var item in Pages)
            {
                _stack.Push(item);
                found = FindNavigationItemsHierarchyFromPageType(pageType, item.Items, ref _stack);
                if (found)
                {
                    break;
                }
                _stack.Pop();
            }

            while (_stack.Count > 0)
            {
                _revStack.Push(_stack.Pop());
            }

            foreach (var item in _revStack)
            {
                list.Add(item);
            }

            return list;
        }

        private bool FindNavigationItemsHierarchyFromPageType(Type pageType, ICollection<PagesInfoDataItem> pages, ref Stack<PagesInfoDataItem> stack)
        {
            var item = stack.Peek();
            bool found = false;

            if (pageType == item.PageType)
            {
                return true;
            }

            foreach (var child in item.Items)
            {
                stack.Push(child);
                found = FindNavigationItemsHierarchyFromPageType(pageType, child.Items, ref stack);
                if (found) { return true; }
                stack.Pop();
            }

            return false;
        }

        public void UpdateCanNavigateBack()
        {
            CanNavigateback = _navigationService.IsBackHistoryNonEmpty();
        }

        #endregion
    }
}
