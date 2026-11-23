namespace GameControllerForZwift.UnitTests.Theme
{
    /// <summary>Finds the files of the repo that the spec tests read.</summary>
    internal static class RepoPaths
    {
        public static string Root()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "docs", "spec", "ux", "visual-system.md")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new DirectoryNotFoundException("docs/spec/ux/visual-system.md not found above " + AppContext.BaseDirectory);
        }

        public static string VisualSystemSpec() => Path.Combine(Root(), "docs", "spec", "ux", "visual-system.md");

        public static string App() => Path.Combine(Root(), "windows", "src", "Presentation", "GameControllerForZwift");

        public static string Resource(string name) => Path.Combine(App(), "Resources", name);

        public static string TestSource(string relative) => Path.Combine(Root(), "windows", "tests", "GameControllerForZwift.UnitTests", relative);
    }
}
