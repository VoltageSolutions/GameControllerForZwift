using GameControllerForZwift.UI.WPF.Navigation;
using System.Windows;
using System.Windows.Controls;

namespace GameControllerForZwift.UI.WPF.Tests.Navigation
{
    public class ViewCacheTests
    {
        private sealed class VmA { }

        private sealed class VmB { }

        private int _created;

        private ViewCache CreateCache() => new(new Dictionary<Type, Func<FrameworkElement>>
        {
            [typeof(VmA)] = () =>
            {
                _created++;
                return new UserControl();
            },
            [typeof(VmB)] = () => new UserControl(),
        });

        [WpfFact]
        public void GetView_CreatesTheViewOnceAndReusesIt()
        {
            ViewCache cache = CreateCache();
            var vm = new VmA();

            FrameworkElement? first = cache.GetView(vm);
            FrameworkElement? second = cache.GetView(vm);

            Assert.NotNull(first);
            Assert.Same(first, second);
            Assert.Equal(1, _created);
        }

        [WpfFact]
        public void GetView_SetsTheViewModelAsDataContext()
        {
            var vm = new VmA();

            FrameworkElement? view = CreateCache().GetView(vm);

            Assert.Same(vm, view!.DataContext);
        }

        [WpfFact]
        public void GetView_ReturnsDifferentViewsForDifferentViewModels()
        {
            ViewCache cache = CreateCache();

            Assert.NotSame(cache.GetView(new VmA()), cache.GetView(new VmB()));
        }

        [WpfFact]
        public void GetView_ReturnsNull_ForNullOrUnknownViewModel()
        {
            ViewCache cache = CreateCache();

            Assert.Null(cache.GetView(null));
            Assert.Null(cache.GetView(new object()));
        }
    }
}
