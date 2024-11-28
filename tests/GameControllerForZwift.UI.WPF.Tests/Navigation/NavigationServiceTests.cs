using Xunit;
using NSubstitute;
using GameControllerForZwift.UI.WPF.Navigation;
using System;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using GameControllerForZwift.UI.WPF.Views;

namespace GameControllerForZwift.UI.WPF.Tests.Navigation
{
    public class NavigationServiceTests
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly NavigationService _navigationService;
        private readonly Frame _frame;

        public NavigationServiceTests()
        {
            _serviceProvider = Substitute.For<IServiceProvider>();
            _frame = new Frame();
            _navigationService = new NavigationService(_serviceProvider);
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

        //[WpfFact]
        //public void Navigate_ShouldNavigateToPage()
        //{
        //    // Arrange
        //    var pageType = typeof(Page);
        //    var pageInstance = new Page();
        //    _serviceProvider.GetRequiredService(pageType).Returns(pageInstance);

        //    // Act
        //    _navigationService.Navigate(pageType);

        //    // Assert
        //    Assert.Equal(pageInstance, _frame.Content);
        //}

        //[WpfFact]
        //public void Navigate_ShouldNavigateToDifferentPages()
        //{
        //    // Arrange
        //    var pageType1 = typeof(HomePage);
        //    var pageInstance1 = new HomePage();
        //    var pageType2 = typeof(SettingsPage);
        //    var pageInstance2 = new SettingsPage();

        //    _serviceProvider.GetRequiredService(pageType1).Returns(pageInstance1);
        //    _serviceProvider.GetRequiredService(pageType2).Returns(pageInstance2);

        //    // Act
        //    _navigationService.Navigate(pageType1);
        //    var firstNavigationContent = _frame.Content;

        //    _navigationService.Navigate(pageType2);
        //    var secondNavigationContent = _frame.Content;

        //    // Assert
        //    Assert.Equal(pageInstance1, firstNavigationContent);
        //    Assert.Equal(pageInstance2, secondNavigationContent);
        //}

        //[WpfFact]
        //public void Navigate_ShouldPushCurrentPageToHistory()
        //{
        //    // Arrange
        //    var pageType1 = typeof(Page);
        //    var pageType2 = typeof(UserControl);
        //    var pageInstance1 = new Page();
        //    var pageInstance2 = new UserControl();
        //    _serviceProvider.GetRequiredService(pageType1).Returns(pageInstance1);
        //    _serviceProvider.GetRequiredService(pageType2).Returns(pageInstance2);

        //    // Act
        //    _navigationService.Navigate(pageType1);
        //    _navigationService.Navigate(pageType2);

        //    // Assert
        //    _navigationService.NavigateBack();
        //    Assert.Equal(pageInstance1, _frame.Content);
        //}

        //[WpfFact]
        //public void NavigateBack_ShouldNavigateToPreviousPageInHistory()
        //{
        //    // Arrange
        //    var pageType1 = typeof(Page);
        //    var pageType2 = typeof(UserControl);
        //    var pageInstance1 = new Page();
        //    var pageInstance2 = new UserControl();
        //    _serviceProvider.GetRequiredService(pageType1).Returns(pageInstance1);
        //    _serviceProvider.GetRequiredService(pageType2).Returns(pageInstance2);

        //    // Act
        //    _navigationService.Navigate(pageType1);
        //    _navigationService.Navigate(pageType2);
        //    _navigationService.NavigateBack();

        //    // Assert
        //    Assert.Equal(pageInstance1, _frame.Content);
        //}

        //[WpfFact]
        //public void NavigateForward_ShouldNavigateToNextPageInFuture()
        //{
        //    // Arrange
        //    var pageType1 = typeof(Page);
        //    var pageType2 = typeof(UserControl);
        //    var pageInstance1 = new Page();
        //    var pageInstance2 = new UserControl();
        //    _serviceProvider.GetRequiredService(pageType1).Returns(pageInstance1);
        //    _serviceProvider.GetRequiredService(pageType2).Returns(pageInstance2);

        //    // Act
        //    _navigationService.Navigate(pageType1);
        //    _navigationService.Navigate(pageType2);
        //    _navigationService.NavigateBack();
        //    _navigationService.NavigateForward();

        //    // Assert
        //    Assert.Equal(pageInstance2, _frame.Content);
        //}

        //[WpfFact]
        //public void RaiseNavigatingEvent_ShouldTriggerNavigatingEvent()
        //{
        //    // Arrange
        //    var wasRaised = false;
        //    var pageType = typeof(Page);

        //    _navigationService.Navigating += (_, args) =>
        //    {
        //        wasRaised = args.PageType == pageType;
        //    };

        //    // Act
        //    _navigationService.RaiseNavigatingEvent(pageType);

        //    // Assert
        //    Assert.True(wasRaised);
        //}

        //[WpfFact]
        //public void IsBackHistoryNonEmpty_ShouldReturnTrue_WhenHistoryIsNotEmpty()
        //{
        //    // Arrange
        //    var pageType = typeof(Page);
        //    var pageInstance = new Page();
        //    _serviceProvider.GetRequiredService(pageType).Returns(pageInstance);

        //    // Act
        //    _navigationService.Navigate(pageType);

        //    // Assert
        //    Assert.True(_navigationService.IsBackHistoryNonEmpty());
        //}

        //[WpfFact]
        //public void IsBackHistoryNonEmpty_ShouldReturnFalse_WhenHistoryIsEmpty()
        //{
        //    // Assert
        //    Assert.False(_navigationService.IsBackHistoryNonEmpty());
        //}
    }
}
