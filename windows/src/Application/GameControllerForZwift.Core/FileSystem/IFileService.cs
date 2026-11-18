namespace GameControllerForZwift.Core.FileSystem
{
    public interface IFileService
    {
        string ReadFileContent(string filePath);

        /// <summary>
        /// Writes the file in one step: the content goes to a temporary file that then replaces the target, so readers
        /// (and the config folder watcher) never see a half-written file.
        /// </summary>
        void WriteFileContent(string filePath, string content);

        bool FileExists(string filePath);

        void DeleteFile(string filePath);

        bool DirectoryExists(string directoryPath);

        void CreateDirectory(string directoryPath);

        /// <summary>
        /// Lists the files in a folder (not its subfolders) that match the pattern, or nothing if the folder does not exist.
        /// </summary>
        IReadOnlyList<string> GetFiles(string directoryPath, string searchPattern);
    }
}
