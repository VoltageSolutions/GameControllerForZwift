using GameControllerForZwift.UI.WPF.Models;
using GameControllerForZwift.UI.WPF.Navigation;
using GameControllerForZwift.UI.WPF.Views;
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
            _navigationServiceMock.Received(1).Navigate(typeof(SettingsPage));
        }

        [Fact]
        public void BackCommand_InvokesNavigateBack()
        {
            // Act
            _viewModel.BackCommand.Execute(null);

            // Assert
            _navigationServiceMock.Received(1).NavigateBack();
        }

        [Fact]
        public void ForwardCommand_InvokesNavigateForward()
        {
            // Act
            _viewModel.ForwardCommand.Execute(null);

            // Assert
            _navigationServiceMock.Received(1).NavigateForward();
        }

        [Fact]
        public void UpdateCanNavigateBack_SetsCanNavigatebackToTrue_WhenBackHistoryIsNonEmpty()
        {
            // Arrange
            _navigationServiceMock.IsBackHistoryNonEmpty().Returns(true);

            // Act
            _viewModel.UpdateCanNavigateBack();

            // Assert
            Assert.True(_viewModel.CanNavigateback);
        }

        [Fact]
        public void UpdateCanNavigateBack_SetsCanNavigatebackToFalse_WhenBackHistoryIsEmpty()
        {
            // Arrange
            _navigationServiceMock.IsBackHistoryNonEmpty().Returns(false);

            // Act
            _viewModel.UpdateCanNavigateBack();

            // Assert
            Assert.False(_viewModel.CanNavigateback);
        }

        [Fact]
        public void GetNavigationItemHierarchyFromPageType_ReturnsCorrectHierarchy_WhenPageTypeExists()
        {
            // Arrange
            var childItem = new PagesInfoDataItem { Title = "Child", PageName = nameof(SettingsPage) };
            var parentItem = new PagesInfoDataItem
            {
                Title = "Parent",
                Items = new ObservableCollection<PagesInfoDataItem> { childItem }
            };

            _viewModel.Pages = new List<PagesInfoDataItem> { parentItem };

            // Act
            var result = _viewModel.GetNavigationItemHierarchyFromPageType(typeof(SettingsPage));

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(parentItem, result[0]);
            Assert.Equal(childItem, result[1]);
        }

        [Fact]
        public void GetNavigationItemHierarchyFromPageType_ReturnsEmptyList_WhenPageTypeDoesNotExist()
        {
            // Arrange
            var item = new PagesInfoDataItem { Title = "Item", PageName = nameof(SettingsPage) };
            _viewModel.Pages = new List<PagesInfoDataItem> { item };

            // Act
            var result = _viewModel.GetNavigationItemHierarchyFromPageType(typeof(HomePage));

            // Assert
            Assert.Empty(result);
        }

    }
}
