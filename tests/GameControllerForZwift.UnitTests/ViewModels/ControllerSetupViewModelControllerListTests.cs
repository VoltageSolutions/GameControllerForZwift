using GameControllerForZwift.Core.Configurations;
using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.UI.WPF.ViewModels;
using NSubstitute;
using static GameControllerForZwift.UI.WPF.Tests.ViewModels.ConfigurationTestContext;

namespace GameControllerForZwift.UI.WPF.Tests.ViewModels
{
    /// <summary>
    /// The controller list on Controller Setup (CTRL-001, CTRL-003, CTRL-004).
    /// </summary>
    public sealed class ControllerSetupViewModelControllerListTests : IDisposable
    {
        private static readonly ControllerModel XboxOneS = new(0x045E, 0x02E0, "Xbox One S Controller");

        private readonly ConfigurationTestContext _context = new();

        public void Dispose() => _context.Dispose();

        private void Connect(params IController[] controllers) => _context.DataIntegrator.GetControllers().Returns(controllers.ToList());

        [Fact]
        public void SameVendorDifferentProducts_BothAreListed()
        {
            var seriesX = Controller(Xbox);
            var oneS = Controller(XboxOneS);

            var viewModel = _context.CreateViewModel(seriesX, oneS);

            Assert.Equal([seriesX, oneS], viewModel.Controllers);
        }

        [Fact]
        public void TwoControllersOfTheSameModel_BothAreListed()
        {
            var first = Controller(Xbox);
            var second = Controller(Xbox);

            var viewModel = _context.CreateViewModel(first, second);

            Assert.Equal([first, second], viewModel.Controllers);
        }

        [Fact]
        public void Refresh_ControllerAdded_KeepsTheEntriesAndTheSelection()
        {
            var seriesX = Controller(Xbox);
            var viewModel = _context.CreateViewModel(seriesX);
            var collection = viewModel.Controllers;
            Assert.Same(seriesX, viewModel.SelectedController);

            var oneS = Controller(XboxOneS);
            Connect(seriesX, oneS);
            viewModel.RefreshControllerList();

            Assert.Same(collection, viewModel.Controllers);
            Assert.Equal([seriesX, oneS], viewModel.Controllers);
            Assert.Same(seriesX, viewModel.SelectedController);
        }

        [Fact]
        public void Refresh_NothingChanged_LeavesTheListAlone()
        {
            var seriesX = Controller(Xbox);
            var viewModel = _context.CreateViewModel(seriesX);
            int notifications = 0;
            viewModel.Controllers.CollectionChanged += (_, _) => notifications++;

            viewModel.RefreshControllerList();

            Assert.Equal(0, notifications);
            Assert.Same(seriesX, viewModel.SelectedController);
        }

        [Fact]
        public void Refresh_OtherControllerRemoved_KeepsTheSelection()
        {
            var seriesX = Controller(Xbox);
            var oneS = Controller(XboxOneS);
            var viewModel = _context.CreateViewModel(seriesX, oneS);

            Connect(seriesX);
            viewModel.RefreshControllerList();

            Assert.Equal([seriesX], viewModel.Controllers);
            Assert.Same(seriesX, viewModel.SelectedController);
        }
    }
}
