using GameControllerForZwift.Core;
using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.UI.WPF.ViewModels;
using NSubstitute;
using static GameControllerForZwift.UI.WPF.Tests.ViewModels.ConfigurationTestContext;

namespace GameControllerForZwift.UI.WPF.Tests.ViewModels
{
    /// <summary>
    /// The Buttons screen: rows, the function picker, and selecting a row from the controller (SCR-BTN-001 to SCR-BTN-006).
    /// </summary>
    public sealed class ButtonsScreenTests : IDisposable
    {
        private readonly ConfigurationTestContext _context = new();

        public void Dispose() => _context.Dispose();

        [Fact]
        public void SCRBTN001_CatalogListsEveryInputOnce()
        {
            Assert.Equal(ButtonsCatalog.Inputs.Count, ButtonsCatalog.Inputs.Select(i => i.Input).Distinct().Count());
            Assert.Equal(ButtonsCatalog.Inputs.Count, ButtonsCatalog.Inputs.Select(i => i.Key).Distinct().Count());
        }

        [Fact]
        public void SCRBTN001_RowsCoverTheCatalogAndSplitAcrossTabs()
        {
            var viewModel = _context.CreateViewModel(Xbox);

            Assert.Equal(ButtonsCatalog.Inputs.Count, viewModel.Rows.Count);
            Assert.All(viewModel.ButtonRows, r => Assert.Equal(ButtonsTab.Buttons, r.Info.Tab));
            Assert.All(viewModel.SticksAndTriggersRows, r => Assert.Equal(ButtonsTab.SticksAndTriggers, r.Info.Tab));
            Assert.Equal(viewModel.Rows.Count, viewModel.ButtonRows.Count + viewModel.SticksAndTriggersRows.Count);
        }

        [Fact]
        public void SCRBTN040_SticksAndTriggersFillTwoColumnsLeftThenRight()
        {
            var viewModel = _context.CreateViewModel(Xbox);

            ControllerInput[] order = [.. viewModel.SticksAndTriggersRows.Select(r => r.Input)];

            // Rows pair up across the two columns, so each left input sits beside its right counterpart.
            Assert.Equal(
                [
                    ControllerInput.LeftTrigger, ControllerInput.RightTrigger,
                    ControllerInput.LeftThumbstickClick, ControllerInput.RightThumbstickClick,
                    ControllerInput.LeftThumbstickTiltUp, ControllerInput.RightThumbstickTiltUp,
                    ControllerInput.LeftThumbstickTiltDown, ControllerInput.RightThumbstickTiltDown,
                    ControllerInput.LeftThumbstickTiltLeft, ControllerInput.RightThumbstickTiltLeft,
                    ControllerInput.LeftThumbstickTiltRight, ControllerInput.RightThumbstickTiltRight,
                ],
                order);
        }

        [Fact]
        public void SCRBTN041_ATriggerRowReportsHowFarTheTriggerIsPulled()
        {
            var viewModel = _context.CreateViewModel(Xbox);
            ButtonRowViewModel left = viewModel.Rows.Single(r => r.Input == ControllerInput.LeftTrigger);
            ButtonRowViewModel a = viewModel.Rows.Single(r => r.Input == ControllerInput.A);
            var changed = new List<string?>();
            left.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            Assert.True(left.IsAnalogTrigger);
            Assert.False(a.IsAnalogTrigger);
            Assert.Equal(0, left.PullRatio);

            viewModel.Selector(ControllerInput.LeftTrigger).ControllerData = new ControllerData { LeftTrigger = 32767 / 2 };

            Assert.Equal(0.5, left.PullRatio, 3);
            Assert.Contains(nameof(ButtonRowViewModel.PullRatio), changed);
            Assert.Equal(0, a.PullRatio);
        }

        [Fact]
        public void SCRBTN002_RowShowsTheMappedFunction()
        {
            var viewModel = _context.CreateViewModel(Xbox);
            ButtonRowViewModel row = viewModel.Rows.Single(r => r.Input == ControllerInput.X);

            viewModel.Selector(ControllerInput.X).SelectedZwiftFunction = ZwiftFunction.ShiftUp;

            Assert.Equal(ZwiftFunction.ShiftUp, row.Function);
            Assert.Equal("ButtonsRow_X", row.AutomationId);
            Assert.Contains(row.InputName, row.AccessibleName);
        }

        [Fact]
        public void SCRBTN003_SelectRowOpensThePickerForThatRow()
        {
            var viewModel = _context.CreateViewModel(Xbox);
            ButtonRowViewModel row = viewModel.Rows.Single(r => r.Input == ControllerInput.A);

            viewModel.SelectRowCommand.Execute(row);

            Assert.Same(row, viewModel.SelectedRow);
            Assert.True(row.IsSelected);
            Assert.True(viewModel.FunctionPicker.IsOpen);
            Assert.Same(row, viewModel.FunctionPicker.Target);
        }

        [Fact]
        public void SCRBTN003_SelectingARowOnTheOtherTabSwitchesTab()
        {
            var viewModel = _context.CreateViewModel(Xbox);
            ButtonRowViewModel row = viewModel.Rows.Single(r => r.Input == ControllerInput.LeftTrigger);

            viewModel.SelectRowCommand.Execute(row);

            Assert.True(viewModel.IsSticksTabSelected);
            Assert.False(viewModel.IsButtonsTabSelected);
        }

        [Fact]
        public void SCRBTN004_ChoosingAFunctionMapsTheRowAndClosesThePicker()
        {
            var viewModel = _context.CreateViewModel(Xbox);
            ButtonRowViewModel row = viewModel.Rows.Single(r => r.Input == ControllerInput.B);
            viewModel.SelectRowCommand.Execute(row);
            FunctionItemViewModel item = viewModel.FunctionPicker.Groups.SelectMany(g => g.Items).Single(i => i.Function == ZwiftFunction.ShiftDown);

            viewModel.FunctionPicker.Choose(item);

            Assert.Equal(ZwiftFunction.ShiftDown, viewModel.Selector(ControllerInput.B).SelectedZwiftFunction);
            Assert.Equal(ZwiftFunction.ShiftDown, row.Function);
        }

        [Fact]
        public void SCRBTN004_PickerOmitsEmptyGroupsAndUsesStableIds()
        {
            var viewModel = _context.CreateViewModel(Xbox);

            Assert.All(viewModel.FunctionPicker.Groups, g => Assert.NotEmpty(g.Items));
            Assert.All(viewModel.FunctionPicker.Groups.SelectMany(g => g.Items), i => Assert.Equal("FunctionItem_" + i.Function, i.AutomationId));
        }

        [Fact]
        public void SCRBTN044_ClosingThePickerDeselectsTheRow()
        {
            var viewModel = _context.CreateViewModel(Xbox);
            ButtonRowViewModel row = viewModel.Rows[0];
            viewModel.SelectRowCommand.Execute(row);
            Assert.True(row.IsSelected);

            viewModel.FunctionPicker.CloseCommand.Execute(null);

            Assert.False(viewModel.FunctionPicker.IsOpen);
            Assert.Null(viewModel.SelectedRow);
            Assert.False(row.IsSelected);
        }

        [Fact]
        public void SCRBTN006_ControllerPressLightsTheRowWithoutSelectingOrOpeningThePicker()
        {
            GameControllerForZwift.UnitTests.TestApplication.Run(() =>
            {
                var viewModel = _context.CreateViewModel(Xbox);
                ButtonRowViewModel row = viewModel.Rows.Single(r => r.Input == ControllerInput.A);
                ZwiftFunction before = viewModel.Selector(ControllerInput.A).SelectedZwiftFunction;

                _context.DataIntegrator.InputPolled += Raise.Event<EventHandler<InputStateChangedEventArgs>>(_context.DataIntegrator, new InputStateChangedEventArgs(new ControllerData { A = true }));

                Assert.True(viewModel.CurrentControllerValues!.A);
                Assert.Null(viewModel.SelectedRow);
                Assert.False(viewModel.FunctionPicker.IsOpen);
                Assert.False(row.IsSelected);
                Assert.Equal(before, viewModel.Selector(ControllerInput.A).SelectedZwiftFunction);
            });
        }

        [Fact]
        public void SCRBTN005_HidingTheScreenClosesThePicker()
        {
            var viewModel = _context.CreateViewModel(Xbox);
            viewModel.IsScreenActive = true;
            viewModel.SelectRowCommand.Execute(viewModel.Rows[0]);

            viewModel.IsScreenActive = false;

            Assert.False(viewModel.FunctionPicker.IsOpen);
        }

        [Fact]
        public void SCRBTN007_ResetToDefaultNeedsAController()
        {
            var viewModel = _context.CreateViewModel();

            Assert.False(viewModel.ResetToDefaultCommand.CanExecute(null));
        }

        [Fact]
        public void SCRBTN007_ResetToDefaultRestoresTheDefaultMapping()
        {
            var viewModel = _context.CreateViewModel(Xbox);
            ZwiftFunction original = viewModel.Selector(ControllerInput.A).SelectedZwiftFunction;
            viewModel.Selector(ControllerInput.A).SelectedZwiftFunction = original == ZwiftFunction.ShiftUp ? ZwiftFunction.ShiftDown : ZwiftFunction.ShiftUp;

            viewModel.ResetToDefaultCommand.Execute(null);

            Assert.Equal(original, viewModel.Selector(ControllerInput.A).SelectedZwiftFunction);
        }
    }
}
