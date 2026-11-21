using GameControllerForZwift.Core;
using GameControllerForZwift.Core.Configurations;
using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.Core.FileSystem;
using GameControllerForZwift.Core.Mapping;
using GameControllerForZwift.UI.WPF.ViewModels;
using NSubstitute;

namespace GameControllerForZwift.UI.WPF.Tests.ViewModels
{
    public sealed class ControllerSetupViewModelTests : IDisposable
    {
        private readonly TempFolder _temp = new();
        private readonly IDataIntegrator _dataIntegratorMock;
        private readonly IControllerProfileService _profileServiceMock;
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

            var fileService = new FileService();
            _viewModel = new ControllerSetupViewModel(
                _dataIntegratorMock,
                _profileServiceMock,
                new ConfigurationStore(fileService, _temp.Combine("Profiles")),
                new AppSettingsStore(fileService, _temp.Combine("settings.json")),
                new FakeDialogService(),
                new ControllerModelMatcher());
        }

        public void Dispose() => _temp.Dispose();

        [Fact]
        public void Constructor_ShouldInitializeDefaultValues()
        {
            // Assert
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
        public void DetectControllers_AsksForTheConnectedControllers()
        {
            // Act
            _dataIntegratorMock.Received(1).GetControllers();
            _viewModel.DetectControllers();

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
