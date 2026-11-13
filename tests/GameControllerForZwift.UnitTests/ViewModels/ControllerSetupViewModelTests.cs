using GameControllerForZwift.Core;
using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.Core.Mapping;
using GameControllerForZwift.UI.WPF.ViewModels;
using NSubstitute;

namespace GameControllerForZwift.UI.WPF.Tests.ViewModels
{
    public class ControllerSetupViewModelTests
    {
        private readonly IDataIntegrator _dataIntegratorMock;
        private readonly IControllerProfileService _profileServiceMock;
        private readonly OutputOptions _options = new OutputOptions();
        private readonly ControllerSetupViewModel _viewModel;

        public ControllerSetupViewModelTests()
        {
            _dataIntegratorMock = Substitute.For<IDataIntegrator>();
            _profileServiceMock = Substitute.For<IControllerProfileService>();

            // Setup default return values for mocks
            _dataIntegratorMock.GetControllers().Returns(new List<IController>
            {
                Substitute.For<IController>(),
                Substitute.For<IController>()
            });

            _profileServiceMock.GetDefaultProfile().Returns(new ControllerProfile
            {
                Mappings = new List<InputMapping>
                {
                    new InputMapping { Input = ControllerInput.A, Function = ZwiftFunction.Select },
                    new InputMapping { Input = ControllerInput.B, Function = ZwiftFunction.GoBack }
                }
            });

            _viewModel = new ControllerSetupViewModel(_dataIntegratorMock, _profileServiceMock, _options);
        }

        [Fact]
        public void OutputChoices_DefaultToEmulatorOnly()
        {
            Assert.True(_viewModel.SendToZwiftEmulator);
            Assert.False(_viewModel.SendToKeyboard);
        }

        [Fact]
        public void OutputChoices_WriteThroughToOptions_AndRaiseNotifications()
        {
            var raised = new List<string?>();
            _viewModel.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

            _viewModel.SendToKeyboard = true;
            _viewModel.SendToZwiftEmulator = false;

            Assert.True(_options.SendToKeyboard);
            Assert.False(_options.SendToZwiftEmulator);
            Assert.Contains(nameof(_viewModel.SendToKeyboard), raised);
            Assert.Contains(nameof(_viewModel.SendToZwiftEmulator), raised);
        }

        [Fact]
        public void OutputChoices_InitializeFromPersistedOptions()
        {
            var options = new OutputOptions { SendToZwiftEmulator = false, SendToKeyboard = true };
            var vm = new ControllerSetupViewModel(_dataIntegratorMock, _profileServiceMock, options);
            Assert.False(vm.SendToZwiftEmulator);
            Assert.True(vm.SendToKeyboard);
        }

        [Fact]
        public void Constructor_ShouldInitializeDefaultValues()
        {
            // Assert
            Assert.Equal("Controller Setup", _viewModel.Title);
            Assert.Equal("Select a game controller and configure its button mapping to Zwift functions.", _viewModel.Description);
            Assert.NotNull(_viewModel.ButtonFunctionMappings);
            Assert.NotNull(_viewModel.DpadFunctionMappings);
            Assert.NotNull(_viewModel.LeftStickFunctionMappings);
            Assert.NotNull(_viewModel.RightStickFunctionMappings);
            Assert.NotNull(_viewModel.ShoulderFunctionMappings);
        }

        [Fact]
        public void Constructor_ShouldLoadDefaultProfile()
        {
            // Verify that mappings are applied from the profile
            Assert.Contains(_viewModel.ButtonFunctionMappings, vm => vm.SelectedZwiftFunction == ZwiftFunction.Select);
            Assert.Contains(_viewModel.ButtonFunctionMappings, vm => vm.SelectedZwiftFunction == ZwiftFunction.GoBack);
        }

        [Fact]
        public void RefreshControllerListCommand_ShouldUpdateControllers()
        {
            // Act
            _dataIntegratorMock.Received(1).GetControllers();
            _viewModel.RefreshControllerList();

            // Assert
            Assert.Equal(2, _viewModel.Controllers.Count);
            _dataIntegratorMock.Received(2).GetControllers();
        }

        [Fact]
        public void OnSelectedControllerChanged_ShouldStartProcessingNewController()
        {
            // Arrange
            var oldController = Substitute.For<IController>();
            var newController = Substitute.For<IController>();

            _viewModel.SelectedController = oldController;

            // Act
            _viewModel.SelectedController = newController;

            // Assert
            _dataIntegratorMock.Received().StopProcessing();
            _dataIntegratorMock.Received().StartProcessing(newController);
        }

        [Fact]
        public void OnCurrentControllerValuesChanged_ShouldUpdateMappings()
        {
            // Arrange
            var newData = new ControllerData();

            // Act
            _viewModel.CurrentControllerValues = newData;

            // Assert
            foreach (var mapping in _viewModel.ButtonFunctionMappings)
            {
                Assert.Equal(newData, mapping.ControllerData);
            }
        }
    }
}
