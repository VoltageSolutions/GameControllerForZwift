using GameControllerForZwift.Gamepad.USB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameControllerForZwift.Gamepad.Tests.USB
{
    public class FileServiceTests
    {
        [Fact]
        public void ReadFileContent_ShouldReturnContent_WhenFileExists()
        {
            // Arrange
            var filePath = "./TestData/validfile.txt";
            var expectedContent = "File content here";

            // Mocking File.ReadAllText
            var fileService = new FileService();

            // Assume a file with the path exists and contains 'expectedContent'

            // Act
            var result = fileService.ReadFileContent(filePath);

            // Assert
            Assert.Equal(expectedContent, result);
        }

        [Fact]
        public void ReadFileContent_ShouldThrowFileNotFoundException_WhenFileDoesNotExist()
        {
            // Arrange
            var filePath = "./TestData/nonexistentfile.txt";
            var fileService = new FileService();

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => fileService.ReadFileContent(filePath));
        }

        [Fact]
        public void ReadFileContent_ShouldReturnEmptyString_WhenFileIsEmpty()
        {
            // Arrange
            var filePath = "./TestData/emptyfile.txt";
            var expectedContent = string.Empty;

            var fileService = new FileService();

            // Act
            var result = fileService.ReadFileContent(filePath);

            // Assert
            Assert.Equal(expectedContent, result);
        }
    }
}
