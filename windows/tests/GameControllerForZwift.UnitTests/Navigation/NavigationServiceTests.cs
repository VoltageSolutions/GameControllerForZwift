using GameControllerForZwift.UI.WPF.Navigation;
using Microsoft.Extensions.DependencyInjection;

namespace GameControllerForZwift.UI.WPF.Tests.Navigation
{
    public class NavigationServiceTests
    {
        private sealed class PageA { }

        private sealed class PageB { }

        private sealed class PageC { }

        private readonly NavigationService _service;
        private readonly PageA _pageA = new();
        private readonly PageB _pageB = new();
        private readonly PageC _pageC = new();
        private readonly List<string?> _changes = new();

        public NavigationServiceTests()
        {
            var services = new ServiceCollection();
            services.AddSingleton(_pageA);
            services.AddSingleton(_pageB);
            services.AddSingleton(_pageC);
            _service = new NavigationService(services.BuildServiceProvider());
            _service.PropertyChanged += (_, e) => _changes.Add(e.PropertyName);
        }

        [Fact]
        public void NewService_HasNoCurrentViewModelAndCannotGoBack()
        {
            Assert.Null(_service.CurrentViewModel);
            Assert.False(_service.CanGoBack);
        }

        [Fact]
        public void NavigateTo_FirstPage_ShowsItWithEmptyHistory()
        {
            _service.NavigateTo<PageA>();

            Assert.Same(_pageA, _service.CurrentViewModel);
            Assert.False(_service.CanGoBack);
        }

        [Fact]
        public void NavigateTo_PushesThePreviousPageOntoHistory()
        {
            _service.NavigateTo<PageA>();
            _service.NavigateTo<PageB>();

            Assert.Same(_pageB, _service.CurrentViewModel);
            Assert.True(_service.CanGoBack);

            _service.GoBack();

            Assert.Same(_pageA, _service.CurrentViewModel);
            Assert.False(_service.CanGoBack);
        }

        [Fact]
        public void NavigateTo_CurrentPage_IsNoOp()
        {
            _service.NavigateTo<PageA>();
            _changes.Clear();

            _service.NavigateTo<PageA>();

            Assert.False(_service.CanGoBack);
            Assert.Empty(_changes);
        }

        [Fact]
        public void NavigateTo_AnotherTypeResolvingToTheShownInstance_IsNoOp()
        {
            var services = new ServiceCollection();
            services.AddSingleton(_pageA);
            services.AddSingleton<object>(_pageA);
            var service = new NavigationService(services.BuildServiceProvider());
            service.NavigateTo<PageA>();

            service.NavigateTo<object>();

            Assert.Same(_pageA, service.CurrentViewModel);
            Assert.False(service.CanGoBack);
        }

        [Fact]
        public void NavigateTo_NullType_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _service.NavigateTo(null!));
        }

        [Fact]
        public void GoBack_WalksTheHistoryInReverseOrder()
        {
            _service.NavigateTo<PageA>();
            _service.NavigateTo<PageB>();
            _service.NavigateTo<PageC>();

            _service.GoBack();
            Assert.Same(_pageB, _service.CurrentViewModel);
            Assert.True(_service.CanGoBack);

            _service.GoBack();
            Assert.Same(_pageA, _service.CurrentViewModel);
            Assert.False(_service.CanGoBack);
        }

        [Fact]
        public void GoBack_WithEmptyHistory_IsNoOp()
        {
            _service.NavigateTo<PageA>();
            _changes.Clear();

            _service.GoBack();

            Assert.Same(_pageA, _service.CurrentViewModel);
            Assert.False(_service.CanGoBack);
            Assert.Empty(_changes);
        }

        [Fact]
        public void GoBack_BeforeAnyNavigation_IsNoOp()
        {
            _service.GoBack();

            Assert.Null(_service.CurrentViewModel);
            Assert.False(_service.CanGoBack);
        }

        [Fact]
        public void NavigateTo_AfterGoBack_DoesNotKeepForwardEntries()
        {
            _service.NavigateTo<PageA>();
            _service.NavigateTo<PageB>();
            _service.GoBack();
            _service.NavigateTo<PageC>();

            _service.GoBack();

            Assert.Same(_pageA, _service.CurrentViewModel);
            Assert.False(_service.CanGoBack);
        }

        [Fact]
        public void PropertyChanged_IsRaisedForCurrentViewModelAndCanGoBack()
        {
            _service.NavigateTo<PageA>();
            _changes.Clear();

            _service.NavigateTo<PageB>();

            Assert.Contains(nameof(INavigationService.CurrentViewModel), _changes);
            Assert.Contains(nameof(INavigationService.CanGoBack), _changes);

            _changes.Clear();
            _service.GoBack();

            Assert.Contains(nameof(INavigationService.CurrentViewModel), _changes);
            Assert.Contains(nameof(INavigationService.CanGoBack), _changes);
        }

        [Fact]
        public void GoBack_WhenSelectionSyncNavigatesToTheShownPage_AddsNoHistoryEntry()
        {
            // The shell moves the nav selection when the page changes. A selection handler that navigates to the
            // selected page must not push anything while the service announces the change.
            _service.NavigateTo<PageA>();
            _service.NavigateTo<PageB>();
            _service.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(INavigationService.CurrentViewModel) && _service.CurrentViewModel is not null)
                {
                    _service.NavigateTo(_service.CurrentViewModel.GetType());
                }
            };

            _service.GoBack();

            Assert.Same(_pageA, _service.CurrentViewModel);
            Assert.False(_service.CanGoBack);
        }

        [Fact]
        public void NavigateTo_FromAChangeNotification_IsIgnored()
        {
            _service.NavigateTo<PageA>();
            _service.NavigateTo<PageB>();
            _service.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(INavigationService.CurrentViewModel))
                {
                    _service.NavigateTo<PageC>();
                }
            };

            _service.GoBack();

            Assert.Same(_pageA, _service.CurrentViewModel);
            Assert.False(_service.CanGoBack);
        }

        [Fact]
        public void NavigateToRoot_ShowsThePageAndClearsTheHistory()
        {
            _service.NavigateTo<PageA>();
            _service.NavigateTo<PageB>();
            _changes.Clear();

            _service.NavigateToRoot(typeof(PageC));

            Assert.Same(_pageC, _service.CurrentViewModel);
            Assert.False(_service.CanGoBack);
            Assert.Contains(nameof(INavigationService.CurrentViewModel), _changes);
            Assert.Contains(nameof(INavigationService.CanGoBack), _changes);
        }

        [Fact]
        public void NavigateToRoot_OnTheShownPage_OnlyClearsTheHistory()
        {
            _service.NavigateTo<PageA>();
            _service.NavigateTo<PageB>();
            _changes.Clear();

            _service.NavigateToRoot(typeof(PageB));

            Assert.Same(_pageB, _service.CurrentViewModel);
            Assert.False(_service.CanGoBack);
            Assert.Equal(nameof(INavigationService.CanGoBack), Assert.Single(_changes));
        }

        [Fact]
        public void NavigateToRoot_OnTheShownPageWithoutHistory_RaisesNothing()
        {
            _service.NavigateTo<PageA>();
            _changes.Clear();

            _service.NavigateToRoot(typeof(PageA));

            Assert.Empty(_changes);
        }

        [Fact]
        public void NavigateToRoot_NullType_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _service.NavigateToRoot(null!));
        }

        [Fact]
        public void NavigateToRoot_FromAChangeNotification_IsIgnored()
        {
            _service.NavigateTo<PageA>();
            _service.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(INavigationService.CurrentViewModel))
                {
                    _service.NavigateToRoot(typeof(PageC));
                }
            };

            _service.NavigateTo<PageB>();

            Assert.Same(_pageB, _service.CurrentViewModel);
            Assert.True(_service.CanGoBack);
        }
    }
}
