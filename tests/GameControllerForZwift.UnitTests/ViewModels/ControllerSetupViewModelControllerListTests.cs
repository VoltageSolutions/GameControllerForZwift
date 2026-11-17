using GameControllerForZwift.Core;
using GameControllerForZwift.Core.Configurations;
using GameControllerForZwift.Core.Mapping;
using GameControllerForZwift.UI.WPF.Dialogs;
using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.UI.WPF.ViewModels;
using NSubstitute;
using static GameControllerForZwift.UI.WPF.Tests.ViewModels.ConfigurationTestContext;

namespace GameControllerForZwift.UI.WPF.Tests.ViewModels
{
    /// <summary>
    /// The controller list on Controller Setup and its automatic updates (CTRL-001 to CTRL-008).
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
            viewModel.DetectControllers();

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

            viewModel.DetectControllers();

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
            viewModel.DetectControllers();

            Assert.Equal([seriesX], viewModel.Controllers);
            Assert.Same(seriesX, viewModel.SelectedController);
        }

        [Fact]
        public void SelectedDisconnects_OthersRemain_FirstRemainingIsSelectedAndProcessed()
        {
            // CTRL-005
            var a = Controller(Xbox);
            var b = Controller(XboxOneS);
            var c = Controller(DualSense);
            var viewModel = _context.CreateViewModel(a, b, c);
            Assert.Same(a, viewModel.SelectedController);

            Connect(c, b);
            viewModel.DetectControllers();

            Assert.Equal([b, c], viewModel.Controllers);
            Assert.Same(b, viewModel.SelectedController);
            _context.DataIntegrator.Received(1).StartProcessing(b);
        }

        [Fact]
        public void SelectedDisconnects_NoneRemain_NothingSelectedAndOnlyDefaultListed()
        {
            // CTRL-006
            _context.Seed("Climbing", Xbox);
            var a = Controller(Xbox);
            var viewModel = _context.CreateViewModel(a);
            Assert.Equal(["Default", "Climbing"], viewModel.Names());

            Connect();
            viewModel.DetectControllers();

            Assert.Empty(viewModel.Controllers);
            Assert.Null(viewModel.SelectedController);
            Assert.Null(viewModel.CurrentControllerModel);
            Assert.Equal(["Default"], viewModel.Names());
            Assert.False(viewModel.SaveAsCommand.CanExecute(null));
            Assert.False(viewModel.ImportCommand.CanExecute(null));
            _context.DataIntegrator.Received().StopProcessing();
        }

        [Fact]
        public void NothingConnectedAtStart_ControllerConnects_ItIsSelected()
        {
            // CTRL-007
            var viewModel = _context.CreateViewModel();
            Assert.Null(viewModel.SelectedController);
            Assert.False(viewModel.SaveAsCommand.CanExecute(null));

            var a = Controller(Xbox);
            Connect(a);
            viewModel.DetectControllers();

            Assert.Same(a, viewModel.SelectedController);
            Assert.True(viewModel.SaveAsCommand.CanExecute(null));
        }

        [Fact]
        public void SelectedControllerReturns_WhileAnotherIsSelected_SelectionStays()
        {
            // CTRL-007: a returning controller does not take the selection.
            var a = Controller(Xbox);
            var b = Controller(XboxOneS);
            var viewModel = _context.CreateViewModel(a, b);
            Connect(b);
            viewModel.DetectControllers();
            Assert.Same(b, viewModel.SelectedController);

            var aAgain = Controller(Xbox);
            Connect(b, aAgain);
            viewModel.DetectControllers();

            Assert.Equal([b, aAgain], viewModel.Controllers);
            Assert.Same(b, viewModel.SelectedController);
        }

        [Fact]
        public void SelectedControllerReturns_AfterAllDisconnected_IsSelectedWithItsLastUsedConfiguration()
        {
            // CTRL-006, CTRL-007, CFG-017
            _context.Seed("Climbing", Xbox);
            var a = Controller(Xbox);
            var viewModel = _context.CreateViewModel(a);
            viewModel.Select("Climbing");
            Connect();
            viewModel.DetectControllers();
            Assert.Equal("Default", viewModel.SelectedConfiguration?.Name);

            var aAgain = Controller(Xbox);
            Connect(aAgain);
            viewModel.DetectControllers();

            Assert.Same(aAgain, viewModel.SelectedController);
            Assert.Equal("Climbing", viewModel.SelectedConfiguration?.Name);
        }

        [Fact]
        public void SelectedDisconnects_SameModelRemains_SwitchesWithoutAskingAndKeepsEdits()
        {
            // CTRL-005, CTRL-008: the configurations stay the same, so nothing is lost.
            var a = Controller(Xbox);
            var b = Controller(Xbox);
            var viewModel = _context.CreateViewModel(a, b);
            viewModel.Selector(ControllerInput.Menu).SelectedZwiftFunction = ZwiftFunction.RideOn;
            Assert.True(viewModel.IsDirty);

            Connect(b);
            viewModel.DetectControllers();

            Assert.Same(b, viewModel.SelectedController);
            Assert.Equal(0, _context.Dialogs.UnsavedChangesPrompts);
            Assert.True(viewModel.IsDirty);
        }

        [Fact]
        public void SelectedDisconnects_WithUnsavedEdits_AsksFirst_DiscardSwitches()
        {
            // CTRL-008, CFG-024
            var a = Controller(Xbox);
            var b = Controller(DualSense);
            var viewModel = _context.CreateViewModel(a, b);
            viewModel.Selector(ControllerInput.Menu).SelectedZwiftFunction = ZwiftFunction.RideOn;
            _context.Dialogs.UnsavedChangesAnswers.Enqueue(UnsavedChangesChoice.Discard);

            Connect(b);
            viewModel.DetectControllers();

            Assert.Equal(1, _context.Dialogs.UnsavedChangesPrompts);
            Assert.Same(b, viewModel.SelectedController);
            Assert.Equal(DualSense, viewModel.CurrentControllerModel);
            Assert.False(viewModel.IsDirty);
        }

        [Fact]
        public void SelectedDisconnects_WithUnsavedEdits_Cancel_KeepsEditsWithNoControllerSelected()
        {
            // CTRL-008, CFG-024
            var a = Controller(Xbox);
            var b = Controller(DualSense);
            var viewModel = _context.CreateViewModel(a, b);
            viewModel.Selector(ControllerInput.Menu).SelectedZwiftFunction = ZwiftFunction.RideOn;
            _context.Dialogs.UnsavedChangesAnswers.Enqueue(UnsavedChangesChoice.Cancel);

            Connect(b);
            viewModel.DetectControllers();

            Assert.Equal([b], viewModel.Controllers);
            Assert.Null(viewModel.SelectedController);
            Assert.Equal(Xbox, viewModel.CurrentControllerModel);
            Assert.True(viewModel.IsDirty);
            Assert.Equal(ZwiftFunction.RideOn, viewModel.Selector(ControllerInput.Menu).SelectedZwiftFunction);
        }

        [Fact]
        public void LastDisconnects_WithUnsavedEdits_Cancel_KeepsEditsAndConfigurations()
        {
            // CTRL-006, CTRL-008
            _context.Seed("Climbing", Xbox);
            var a = Controller(Xbox);
            var viewModel = _context.CreateViewModel(a);
            viewModel.Selector(ControllerInput.Menu).SelectedZwiftFunction = ZwiftFunction.RideOn;
            _context.Dialogs.UnsavedChangesAnswers.Enqueue(UnsavedChangesChoice.Cancel);

            Connect();
            viewModel.DetectControllers();

            Assert.Null(viewModel.SelectedController);
            Assert.Equal(["Default", "Climbing"], viewModel.Names());
            Assert.True(viewModel.IsDirty);
            Assert.True(viewModel.SaveAsCommand.CanExecute(null));
        }
    }
}
