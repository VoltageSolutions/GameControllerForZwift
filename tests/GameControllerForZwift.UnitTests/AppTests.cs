using GameControllerForZwift.Core;
using GameControllerForZwift.Core.FileSystem;
using GameControllerForZwift.Core.Mapping;
using GameControllerForZwift.UI.WPF;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace GameControllerForZwift.UnitTests
{
    [Collection(WpfWindows.Name)]
    public class AppTests
    {
        [WpfFact]
        public void ConfigureServices_RegistersAllDependencies()
        {
            // Arrange
            var services = new ServiceCollection();
            TestApplication.Run(() => { });

            // Act
            var configureServicesMethod = typeof(App)
                .GetMethod("ConfigureServices", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            configureServicesMethod?.Invoke(null, new object[] { services });

            // Assert
            var serviceProvider = services.BuildServiceProvider();

            // Ensure key services are registered
            Assert.NotNull(serviceProvider.GetRequiredService<IFileService>());
            Assert.NotNull(serviceProvider.GetRequiredService<IInputService>());
            Assert.NotNull(serviceProvider.GetRequiredService<IControllerProfileService>());
            Assert.NotNull(serviceProvider.GetRequiredService<IDataIntegrator>());
            Assert.NotNull(serviceProvider.GetRequiredService<MainWindowViewModel>());
            //Assert.NotNull(serviceProvider.GetRequiredService<MainWindow>());
        }

        // A fake file service for demonstration purposes
        private sealed class FakeFileService : IFileService
        {
            public string ReadFileContent(string filePath)
            {
                return string.Empty;
            }

            public void WriteFileContent(string filePath, string content) { }

            public bool FileExists(string filePath) => false;

            public void DeleteFile(string filePath) { }

            public bool DirectoryExists(string directoryPath) => false;

            public void CreateDirectory(string directoryPath) { }

            public IReadOnlyList<string> GetFiles(string directoryPath, string searchPattern) => [];
        }
    }
}
