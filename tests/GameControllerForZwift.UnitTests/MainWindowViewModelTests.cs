using GameControllerForZwift.Core.Configurations;
using GameControllerForZwift.UI.WPF.Dialogs;
using GameControllerForZwift.UI.WPF.Navigation;
using GameControllerForZwift.UI.WPF.ViewModels;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Collections.ObjectModel;

namespace GameControllerForZwift.UI.WPF.Tests
{
    public class MainWindowViewModelTests
    {
        private readonly INavigationService _navigationServiceMock;
        private readonly ILogger<MainWindowViewModel> _loggerMock;
        private readonly MainWindowViewModel _viewModel;

        public MainWindowViewModelTests()
        {
            _navigationServiceMock = Substitute.For<INavigationService>();
            _loggerMock = Substitute.For<ILogger<MainWindowViewModel>>();
            _viewModel = new MainWindowViewModel(_navigationServiceMock, _loggerMock);
        }

        private static SettingsViewModel CreateSettingsViewModel() => new(
            Substitute.For<IConfigurationStore>(), Substitute.For<IAppSettingsStore>(), Substitute.For<IDialogService>());

        [Fact]
        public void Constructor_InitializesPagesProperty()
        {
            // Arrange & Act (Setup already initializes the view model)

            // Assert
            Assert.NotNull(_viewModel.Pages);
            Assert.NotEmpty(_viewModel.Pages);
        }

        [Fact]
        public void SettingsCommand_InvokesNavigationToSettingsPage()
        {
            // Act
            _viewModel.SettingsCommand.Execute(null);

            // Assert
            _navigationServiceMock.Received(1).NavigateTo<SettingsViewModel>();
        }

        [Fact]
        public void BackCommand_InvokesGoBack_WhenHistoryIsNotEmpty()
        {
            // Arrange
            _navigationServiceMock.CanGoBack.Returns(true);

            // Act
            _viewModel.BackCommand.Execute(null);

            // Assert
            _navigationServiceMock.Received(1).GoBack();
        }

        [Fact]
        public void BackCommand_CannotExecute_WhenHistoryIsEmpty()
        {
            // Arrange
            _navigationServiceMock.CanGoBack.Returns(false);

            // Act & Assert
            Assert.False(_viewModel.BackCommand.CanExecute(null));
        }

        [Fact]
        public void BackCommand_CanExecuteChanges_WhenNavigationServiceReportsCanGoBackChange()
        {
            // Arrange
            bool raised = false;
            _viewModel.BackCommand.CanExecuteChanged += (_, _) => raised = true;
            _navigationServiceMock.CanGoBack.Returns(true);

            // Act
            _navigationServiceMock.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
                _navigationServiceMock, new System.ComponentModel.PropertyChangedEventArgs(nameof(INavigationService.CanGoBack)));

            // Assert
            Assert.True(raised);
            Assert.True(_viewModel.CanGoBack);
            Assert.True(_viewModel.BackCommand.CanExecute(null));
        }

        [Fact]
        public void CurrentViewModel_RaisesPropertyChanged_WhenNavigationServiceChangesPage()
        {
            // Arrange
            var page = new object();
            var changed = new List<string?>();
            _viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
            _navigationServiceMock.CurrentViewModel.Returns(page);

            // Act
            _navigationServiceMock.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
                _navigationServiceMock, new System.ComponentModel.PropertyChangedEventArgs(nameof(INavigationService.CurrentViewModel)));

            // Assert
            Assert.Contains(nameof(MainWindowViewModel.CurrentViewModel), changed);
            Assert.Same(page, _viewModel.CurrentViewModel);
        }

        [Fact]
        public void GetNavigationItemHierarchyFromViewModelType_ReturnsEmpty_ForSettingsBecauseItIsNotInTheTree()
        {
            // The real navigation tree has no Settings item, so the shell must show no tree selection for it.
            Assert.Empty(_viewModel.GetNavigationItemHierarchyFromViewModelType(typeof(SettingsViewModel)));
            Assert.NotEmpty(_viewModel.GetNavigationItemHierarchyFromViewModelType(typeof(ControllerSetupViewModel)));
        }

        [Fact]
        public void IsSettingsShown_FollowsTheCurrentViewModel()
        {
            var changed = new List<string?>();
            _viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            _navigationServiceMock.CurrentViewModel.Returns(CreateSettingsViewModel());
            _navigationServiceMock.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
                _navigationServiceMock, new System.ComponentModel.PropertyChangedEventArgs(nameof(INavigationService.CurrentViewModel)));

            Assert.True(_viewModel.IsSettingsShown);
            Assert.Contains(nameof(MainWindowViewModel.IsSettingsShown), changed);

            _navigationServiceMock.CurrentViewModel.Returns(new object());
            Assert.False(_viewModel.IsSettingsShown);
        }

        [Fact]
        public void NavigateToStartPage_NavigatesToControllerSetup()
        {
            // Act
            _viewModel.NavigateToStartPage();

            // Assert
            _navigationServiceMock.Received(1).NavigateTo<ControllerSetupViewModel>();
        }

        [Fact]
        public void NavigateToPage_NavigatesToTheItemsViewModelType()
        {
            // Arrange
            var item = new PagesInfoDataItem { ViewModelName = nameof(AboutPageViewModel) };

            // Act
            _viewModel.NavigateToPage(item);

            // Assert
            _navigationServiceMock.Received(1).NavigateTo(Arg.Is<Type>(t => t == item.ViewModelType));
        }

        [Fact]
        public void NavigateToPage_DoesNothing_ForNullOrUnknownItem()
        {
            // Act
            _viewModel.NavigateToPage(null);
            _viewModel.NavigateToPage(new PagesInfoDataItem { ViewModelName = "DoesNotExist" });

            // Assert
            _navigationServiceMock.DidNotReceiveWithAnyArgs().NavigateTo(default!);
        }

        [Fact]
        public void GetNavigationItemHierarchyFromViewModelType_ReturnsCorrectHierarchy_WhenPageTypeExists()
        {
            // Arrange
            var childItem = new PagesInfoDataItem { Title = "Child", ViewModelName = nameof(SettingsViewModel) };
            var parentItem = new PagesInfoDataItem
            {
                Title = "Parent",
                Items = new ObservableCollection<PagesInfoDataItem> { childItem }
            };

            _viewModel.Pages = new List<PagesInfoDataItem> { parentItem };

            // Act
            var result = _viewModel.GetNavigationItemHierarchyFromViewModelType(typeof(SettingsViewModel));

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(parentItem, result[0]);
            Assert.Equal(childItem, result[1]);
        }

        [Fact]
        public void GetNavigationItemHierarchyFromViewModelType_ReturnsEmptyList_WhenPageTypeDoesNotExist()
        {
            // Arrange
            var item = new PagesInfoDataItem { Title = "Item", ViewModelName = nameof(SettingsViewModel) };
            _viewModel.Pages = new List<PagesInfoDataItem> { item };

            // Act
            var result = _viewModel.GetNavigationItemHierarchyFromViewModelType(typeof(AboutPageViewModel));

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetNavigationItemHierarchyFromViewModelType_ReturnsSingleItem_WhenPageIsAtTopLevel()
        {
            // Arrange
            var other = new PagesInfoDataItem { Title = "Other", ViewModelName = nameof(AboutPageViewModel) };
            var top = new PagesInfoDataItem { Title = "Top", ViewModelName = nameof(SettingsViewModel) };
            _viewModel.Pages = new List<PagesInfoDataItem> { other, top };

            // Act
            var result = _viewModel.GetNavigationItemHierarchyFromViewModelType(typeof(SettingsViewModel));

            // Assert
            Assert.Single(result);
            Assert.Same(top, result[0]);
        }

        [Fact]
        public void GetNavigationItemHierarchyFromViewModelType_ReturnsFullHierarchy_WhenPageIsNestedTwoLevelsDeep()
        {
            // Arrange
            var grandchild = new PagesInfoDataItem { Title = "Grandchild", ViewModelName = nameof(SettingsViewModel) };
            var sibling = new PagesInfoDataItem { Title = "Sibling", ViewModelName = nameof(AboutPageViewModel) };
            var child = new PagesInfoDataItem
            {
                Title = "Child",
                Items = new ObservableCollection<PagesInfoDataItem> { grandchild }
            };
            var parent = new PagesInfoDataItem
            {
                Title = "Parent",
                Items = new ObservableCollection<PagesInfoDataItem> { sibling, child }
            };
            _viewModel.Pages = new List<PagesInfoDataItem> { parent };

            // Act
            var result = _viewModel.GetNavigationItemHierarchyFromViewModelType(typeof(SettingsViewModel));

            // Assert
            Assert.Equal(new[] { parent, child, grandchild }, result);
        }

        [Fact]
        public void GetNavigationItemHierarchyFromViewModelType_ReturnsEmptyList_WhenNestedTreeDoesNotContainPageType()
        {
            // Arrange
            var grandchild = new PagesInfoDataItem { Title = "Grandchild", ViewModelName = nameof(AboutPageViewModel) };
            var child = new PagesInfoDataItem
            {
                Title = "Child",
                Items = new ObservableCollection<PagesInfoDataItem> { grandchild }
            };
            var parent = new PagesInfoDataItem
            {
                Title = "Parent",
                Items = new ObservableCollection<PagesInfoDataItem> { child }
            };
            _viewModel.Pages = new List<PagesInfoDataItem> { parent };

            // Act
            var result = _viewModel.GetNavigationItemHierarchyFromViewModelType(typeof(SettingsViewModel));

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetNavigationItemHierarchyFromViewModelType_ReturnsEmptyList_WhenPageTypeIsNull()
        {
            // Act
            var result = _viewModel.GetNavigationItemHierarchyFromViewModelType(null);

            // Assert
            Assert.Empty(result);
        }
    }
}
