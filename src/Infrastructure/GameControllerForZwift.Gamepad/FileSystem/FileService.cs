namespace GameControllerForZwift.Gamepad.FileSystem
{
    public class FileService : IFileService
    {
        public string ReadFileContent(string filePath)
        {
            return File.ReadAllText(filePath);
        }
    }
}
