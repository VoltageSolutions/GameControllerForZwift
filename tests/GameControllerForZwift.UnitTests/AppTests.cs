using GameControllerForZwift.Core;
using GameControllerForZwift.Core.FileSystem;
using GameControllerForZwift.Core.Mapping;
using GameControllerForZwift.UI.WPF;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace GameControllerForZwift.UnitTests
{
    public class AppTests
    {
        [WpfFact]
        public void ConfigureServices_RegistersAllDependencies()
        {
            // Arrange
            var services = new ServiceCollection();
            var app = new App();

            // Act
            var configureServicesMethod = typeof(App)
                .GetMethod("ConfigureServices", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            configureServicesMethod?.Invoke(app, new object[] { services });

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
        private class FakeFileService : IFileService
        {
            public string ReadFileContent(string filePath)
            {
                return string.Empty;
            }
        }
    }
}
