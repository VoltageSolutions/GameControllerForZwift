using GameControllerForZwift.Core;
using GameControllerForZwift.Core.Configurations;
using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.Core.FileSystem;
using GameControllerForZwift.Core.Mapping;
using GameControllerForZwift.UI.WPF.ViewModels;
using NSubstitute;

namespace GameControllerForZwift.UI.WPF.Tests.ViewModels
{
    /// <summary>
    /// A configuration store and settings file in a temp folder, fake dialogs, and a factory for view models over them.
    /// </summary>
    internal sealed class ConfigurationTestContext : IDisposable
    {
        public static readonly ControllerModel Xbox = new(0x045E, 0x0B13, "Xbox Series X Controller");
        public static readonly ControllerModel DualSense = new(0x054C, 0x0CE6, "DualSense Wireless Controller");

        private readonly TempFolder _temp = new();
        private readonly FileService _fileService = new();

        public ConfigurationTestContext()
        {
            Store = new ConfigurationStore(_fileService, ConfigurationsFolder);
            Settings = new AppSettingsStore(_fileService, SettingsFile);

            ProfileService = Substitute.For<IControllerProfileService>();
            ProfileService.GetDefaultProfile().Returns(_ => new ControllerProfile
            {
                Mappings =
                [
                    new InputMapping(ControllerInput.A, ZwiftFunction.Select),
                    new InputMapping(ControllerInput.B, ZwiftFunction.GoBack),
                ],
            });
        }

        public string ConfigurationsFolder => _temp.Combine("Profiles");

        public string SettingsFile => _temp.Combine("settings.json");

        public ConfigurationStore Store { get; }

        public AppSettingsStore Settings { get; }

        public FakeDialogService Dialogs { get; } = new();

        public ControllerModelMatcher Matcher { get; } = new();

        public IControllerProfileService ProfileService { get; }

        public IDataIntegrator DataIntegrator { get; } = Substitute.For<IDataIntegrator>();

        public static IController Controller(ControllerModel model) => TestControllers.Create(model.VendorId, model.ProductId, model.Name);

        /// <summary>
        /// Creates a view model. The first controller is selected, as in the app.
        /// </summary>
        public ControllerSetupViewModel CreateViewModel(params IController[] controllers)
        {
            DataIntegrator.GetControllers().Returns(controllers.ToList());
            return new ControllerSetupViewModel(DataIntegrator, ProfileService, Store, Settings, Dialogs, Matcher);
        }

        public ControllerSetupViewModel CreateViewModel(ControllerModel model) => CreateViewModel(Controller(model));

        /// <summary>
        /// Saves a configuration straight to the store, as if an earlier session had saved it.
        /// </summary>
        public SavedConfiguration Seed(string name, ControllerModel model, ZwiftFunction functionForX = ZwiftFunction.ShiftUp)
        {
            var mappings = ConfigurationMappings.Normalize(
            [
                new(ControllerInput.A, ZwiftFunction.Select),
                new(ControllerInput.X, functionForX),
            ]);
            return Store.Save(new SavedConfiguration(Guid.NewGuid(), name, model, mappings, DateTimeOffset.UtcNow));
        }

        /// <summary>
        /// Writes a configuration file outside the config folder, as if it came from another machine.
        /// </summary>
        public string WriteExternalFile(SavedConfiguration configuration)
        {
            var path = _temp.Combine(Guid.NewGuid().ToString("N") + ".json");
            File.WriteAllText(path, ConfigurationSerializer.Serialize(configuration));
            return path;
        }

        public string WriteExternalFile(string json)
        {
            var path = _temp.Combine(Guid.NewGuid().ToString("N") + ".json");
            File.WriteAllText(path, json);
            return path;
        }

        public string[] ConfigurationFiles() =>
            Directory.Exists(ConfigurationsFolder) ? Directory.GetFiles(ConfigurationsFolder, "*.json") : [];

        public string KeyFor(ControllerModel model) => Matcher.GetKey(model);

        public void Dispose() => _temp.Dispose();
    }

    internal static class ControllerSetupViewModelExtensions
    {
        public static ZwiftFunctionSelectorViewModel Selector(this ControllerSetupViewModel viewModel, ControllerInput input) =>
            viewModel.ButtonFunctionMappings
                .Concat(viewModel.DpadFunctionMappings)
                .Concat(viewModel.LeftStickFunctionMappings)
                .Concat(viewModel.RightStickFunctionMappings)
                .Concat(viewModel.ShoulderFunctionMappings)
                .Single(s => s.SelectedInput == input);

        public static void Select(this ControllerSetupViewModel viewModel, string name) =>
            viewModel.SelectedConfiguration = viewModel.Configurations.Single(c => c.Name == name);

        public static string[] Names(this ControllerSetupViewModel viewModel) =>
            viewModel.Configurations.Select(c => c.Name).ToArray();
    }
}
