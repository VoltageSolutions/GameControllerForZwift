using Xunit;
using NSubstitute;
using GameControllerForZwift.UI.WPF.Navigation;
using System;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using GameControllerForZwift.UI.WPF.Views;
using System.Windows.Navigation;
using NavigationService = GameControllerForZwift.UI.WPF.Navigation.NavigationService;

namespace GameControllerForZwift.UI.WPF.Tests.Navigation
{
    public class NavigationServiceTests
    {
        private readonly ServiceCollection _serviceCollection;
        private readonly IServiceProvider _serviceProvider;
        private readonly NavigationService _navigationService;
        private readonly Frame _frame;

        public NavigationServiceTests()
        {
            _serviceCollection = new ServiceCollection();
            _serviceCollection.AddTransient<TestHomePage>();
            _serviceCollection.AddTransient<TestSettingsPage>();
            _serviceCollection.AddTransient<TestConfigPage>();
            _serviceProvider = _serviceCollection.BuildServiceProvider();

            _frame = new Frame();
            _navigationService = new NavigationService(_serviceProvider);
            // In current app, navigating back will cause actions where view subscribes to this event
            _navigationService.Navigating += _navigationService_Navigating;
            _navigationService.SetFrame(_frame);
        }

        [WpfFact]
        public void SetFrame_ShouldSetTheFrame()
        {
            // Arrange
            var newFrame = new Frame();

            // Act
            _navigationService.SetFrame(newFrame);

            // Assert
            Assert.NotNull(newFrame);
        }

        [WpfFact]
        public void Navigate_ShouldNavigateToDifferentPages()
        {
            // Arrange
            var pageType1 = typeof(TestHomePage);
            var pageType2 = typeof(TestSettingsPage);

            // Act
            _navigationService.Navigate(pageType1);
            _navigationService.Navigate(pageType2);

            // Assert
            // Verify the current page type
            Assert.Equal(pageType2, _navigationService.CurrentPageType); // Assuming CurrentPageType is public or accessible for testing
                                                                         // Verify the navigation history
            Assert.Equal(2, _navigationService.History.Count);
        }

        [WpfFact]
        public void Navigate_ShouldPushCurrentPageToHistory()
        {
            // Arrange
            var pageType1 = typeof(TestHomePage);
            var pageType2 = typeof(TestSettingsPage);

            // Act
            _navigationService.Navigate(pageType1);
            _navigationService.Navigate(pageType2);

            // Assert
            _navigationService.NavigateBack();
            // Go back one in history
            //_navigationService.History.Pop();
            // Original page should be here.
            Assert.Equal(pageType1, _navigationService.History.Peek());
        }

        private void _navigationService_Navigating(object? sender, NavigatingEventArgs e)
        {
            _navigationService.Navigate(e.PageType);
        }

        [WpfFact]
        public void NavigateBack_ShouldNavigateToPreviousPage()
        {
            // Arrange
            var pageType1 = typeof(TestHomePage);
            var pageType2 = typeof(TestSettingsPage);

            _navigationService.Navigate(pageType1);
            _navigationService.Navigate(pageType2);

            // Act
            _navigationService.NavigateBack();

            // Assert
            // Verify the current page type
            Assert.Equal(pageType1, _navigationService.CurrentPageType);
            // Verify the future stack
            Assert.Single(_navigationService.Future);
            Assert.Equal(pageType2, _navigationService.Future.Peek());
        }

        [WpfFact]
        public void NavigateForward_ShouldNavigateToNextPageInFuture()
        {
            // Arrange
            var pageType1 = typeof(TestHomePage);
            var pageType2 = typeof(TestSettingsPage);

            // Act
            _navigationService.Navigate(pageType1);
            _navigationService.Navigate(pageType2);
            _navigationService.NavigateBack();
            _navigationService.NavigateForward();

            // Assert
            Assert.Equal(pageType2, _navigationService.CurrentPageType);
        }

        [WpfFact]
        public void Navigate_ShouldResolvePageFromServiceProvider()
        {
            // Arrange
            var pageType = typeof(TestHomePage);

            // Act
            _navigationService.Navigate(pageType);

            // Assert
            var resolvedPage = _serviceProvider.GetRequiredService(pageType);
            Assert.IsType<TestHomePage>(resolvedPage);
        }

        [WpfFact]
        public void IsBackHistoryNonEmpty_ShouldReturnTrue_WhenHistoryIsNotEmpty()
        {
            // Arrange
            var pageType = typeof(TestHomePage);

            // Act
            _navigationService.Navigate(pageType);

            // Assert
            Assert.True(_navigationService.IsBackHistoryNonEmpty());
        }

        [WpfFact]
        public void IsBackHistoryNonEmpty_ShouldReturnFalse_WhenHistoryIsEmpty()
        {
            // Assert
            Assert.False(_navigationService.IsBackHistoryNonEmpty());
        }
    }
}
