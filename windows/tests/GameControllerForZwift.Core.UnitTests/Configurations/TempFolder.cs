namespace GameControllerForZwift.Core.Tests.Configurations
{
    /// <summary>
    /// A unique folder under the temp path, deleted on dispose. Tests never touch the real %LOCALAPPDATA%.
    /// </summary>
    internal sealed class TempFolder : IDisposable
    {
        public TempFolder()
        {
            Path = System.IO.Path.Join(System.IO.Path.GetTempPath(), "GameControllerForZwift.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string Combine(params string[] parts) => System.IO.Path.Join([Path, .. parts]);

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch (IOException)
            {
                // Best effort: a leftover temp folder does not affect other tests.
            }
        }
    }
}
