using GameControllerForZwift.Core;
using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.UI.WPF.ViewModels;
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
            Assert.Equal(viewModel.Rows.Count, viewModel.ButtonRows.Count + viewModel.LeftStickRows.Count + viewModel.RightStickRows.Count + viewModel.TriggerRows.Count);
        }

        [Fact]
        public void SCRBTN002_RowShowsTheMappedFunction()
        {
            var viewModel = _context.CreateViewModel(Xbox);
            ButtonRowViewModel row = viewModel.Rows.Single(r => r.Input == ControllerInput.X);

            viewModel.Selector(ControllerInput.X).SelectedFunction = ZwiftFunction.ShiftUp;

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

            Assert.Equal(ZwiftFunction.ShiftDown, viewModel.Selector(ControllerInput.B).SelectedFunction);
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
        public void SCRBTN005_ClosingThePickerKeepsTheRowSelected()
        {
            var viewModel = _context.CreateViewModel(Xbox);
            ButtonRowViewModel row = viewModel.Rows.First();
            viewModel.SelectRowCommand.Execute(row);

            viewModel.FunctionPicker.CloseCommand.Execute(null);

            Assert.False(viewModel.FunctionPicker.IsOpen);
        }

        [Fact]
        public void SCRBTN006_PressSelectsRowOnlyWhileTheScreenIsActive()
        {
            var viewModel = _context.CreateViewModel(Xbox);
            var idle = new ControllerData();
            var pressed = new ControllerData { A = true };

            viewModel.IsScreenActive = false;
            viewModel.HandleControllerPress(idle, pressed);
            Assert.Null(viewModel.SelectedRow);

            viewModel.IsScreenActive = true;
            viewModel.HandleControllerPress(idle, pressed);
            Assert.Equal(ControllerInput.A, viewModel.SelectedRow?.Input);
        }

        [Fact]
        public void SCRBTN006_HeldButtonDoesNotReselect()
        {
            var viewModel = _context.CreateViewModel(Xbox);
            viewModel.IsScreenActive = true;
            var held = new ControllerData { A = true };

            viewModel.HandleControllerPress(held, held);

            Assert.Null(viewModel.SelectedRow);
        }

        [Fact]
        public void SCRBTN006_PressDoesNotChangeTheMapping()
        {
            var viewModel = _context.CreateViewModel(Xbox);
            viewModel.IsScreenActive = true;
            ZwiftFunction before = viewModel.Selector(ControllerInput.A).SelectedFunction;

            viewModel.HandleControllerPress(new ControllerData(), new ControllerData { A = true });

            Assert.Equal(before, viewModel.Selector(ControllerInput.A).SelectedFunction);
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
            ZwiftFunction original = viewModel.Selector(ControllerInput.A).SelectedFunction;
            viewModel.Selector(ControllerInput.A).SelectedFunction = original == ZwiftFunction.ShiftUp ? ZwiftFunction.ShiftDown : ZwiftFunction.ShiftUp;

            viewModel.ResetToDefaultCommand.Execute(null);

            Assert.Equal(original, viewModel.Selector(ControllerInput.A).SelectedFunction);
        }

        [Fact]
        public void FirstNewPress_ReturnsNullWhenNothingStarted()
        {
            Assert.Null(ButtonsCatalog.FirstNewPress(null, new ControllerData()));
        }

        [Fact]
        public void FirstNewPress_ReturnsTheNewlyPressedInput()
        {
            Assert.Equal(ControllerInput.B, ButtonsCatalog.FirstNewPress(new ControllerData(), new ControllerData { B = true }));
        }
    }
}
