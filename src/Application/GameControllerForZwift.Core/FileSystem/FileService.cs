namespace GameControllerForZwift.Core.FileSystem
{
    public class FileService : IFileService
    {
        public string ReadFileContent(string filePath)
        {
            return File.ReadAllText(filePath);
        }

        public void WriteFileContent(string filePath, string content)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var tempPath = filePath + ".tmp";
            File.WriteAllText(tempPath, content);
            File.Move(tempPath, filePath, overwrite: true);
        }

        public bool FileExists(string filePath) => File.Exists(filePath);

        public void DeleteFile(string filePath) => File.Delete(filePath);

        public bool DirectoryExists(string directoryPath) => Directory.Exists(directoryPath);

        public void CreateDirectory(string directoryPath) => Directory.CreateDirectory(directoryPath);

        public IReadOnlyList<string> GetFiles(string directoryPath, string searchPattern)
        {
            return Directory.Exists(directoryPath)
                ? Directory.GetFiles(directoryPath, searchPattern, SearchOption.TopDirectoryOnly)
                : [];
        }
    }
}
