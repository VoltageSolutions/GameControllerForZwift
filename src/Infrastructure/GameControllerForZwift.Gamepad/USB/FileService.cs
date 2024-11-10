namespace GameControllerForZwift.Gamepad.USB
{
    public class FileService : IFileService
    {
        public string ReadFileContent(string filePath)
        {
            return File.ReadAllText(filePath);
        }
    }
}
