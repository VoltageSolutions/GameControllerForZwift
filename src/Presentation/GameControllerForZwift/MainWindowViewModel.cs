using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameControllerForZwift.UI.WPF.Navigation;
using GameControllerForZwift.UI.WPF.ViewModels;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

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

        // The following may need to go to other VMs instead
        private readonly ILogger<MainWindowViewModel> _logger;
        #endregion

        #region Constructor

        public MainWindowViewModel(INavigationService navigationService, ILogger<MainWindowViewModel> logger)
        {
            // Setup Navigation
            _pages = PagesInfoDataSource.Instance.PagesInfo;
            _navigationService = navigationService;
            _navigationService.PropertyChanged += OnNavigationServicePropertyChanged;

            // Other DI
            _logger = logger;
        }

        #endregion

        #region Navigation

        /// <summary>The view model of the page being shown. The shell shows the matching view from its view cache.</summary>
        public object? CurrentViewModel => _navigationService.CurrentViewModel;

        /// <summary>True when there is a previous page to return to.</summary>
        public bool CanGoBack => _navigationService.CanGoBack;

        /// <summary>Shows the first page. Starts with an empty history.</summary>
        public void NavigateToStartPage()
        {
            _navigationService.NavigateTo<ControllerSetupViewModel>();
        }

        /// <summary>Shows the page of a navigation item the user picked. Does nothing for items without a page.</summary>
        public void NavigateToPage(PagesInfoDataItem? item)
        {
            if (item?.ViewModelType is Type viewModelType)
            {
                _navigationService.NavigateTo(viewModelType);
            }
        }

        private void OnNavigationServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(INavigationService.CurrentViewModel):
                    OnPropertyChanged(nameof(CurrentViewModel));
                    break;
                case nameof(INavigationService.CanGoBack):
                    OnPropertyChanged(nameof(CanGoBack));
                    BackCommand.NotifyCanExecuteChanged();
                    break;
            }
        }

        #endregion

        #region Commands
        [RelayCommand]
        public void Settings()
        {
            _navigationService.NavigateTo<SettingsViewModel>();
        }

        [RelayCommand(CanExecute = nameof(CanGoBack))]
        public void Back()
        {
            _navigationService.GoBack();
        }

        public List<PagesInfoDataItem> GetNavigationItemHierarchyFromViewModelType(Type? viewModelType)
        {
            List<PagesInfoDataItem> list = new List<PagesInfoDataItem>();
            Stack<PagesInfoDataItem> _stack = new Stack<PagesInfoDataItem>();
            Stack<PagesInfoDataItem> _revStack = new Stack<PagesInfoDataItem>();

            if (viewModelType == null)
            {
                return list;
            }
            foreach (var item in Pages)
            {
                _stack.Push(item);
                bool found = FindNavigationItemsHierarchyFromViewModelType(viewModelType, item.Items, ref _stack);
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

        private static bool FindNavigationItemsHierarchyFromViewModelType(Type viewModelType, ICollection<PagesInfoDataItem> pages, ref Stack<PagesInfoDataItem> stack)
        {
            var item = stack.Peek();
            if (viewModelType == item.ViewModelType)
            {
                return true;
            }

            foreach (var child in item.Items)
            {
                stack.Push(child);
                if (FindNavigationItemsHierarchyFromViewModelType(viewModelType, child.Items, ref stack)) { return true; }
                stack.Pop();
            }

            return false;
        }

        #endregion
    }
}
