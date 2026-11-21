namespace GameControllerForZwift.UI.WPF
{
    /// <summary>Facts about how the app was built.</summary>
    internal static class BuildInfo
    {
        /// <summary>True in Debug builds. Developer-only features, such as the style gallery, check it.</summary>
#if DEBUG
        public const bool IsDebug = true;
#else
        public const bool IsDebug = false;
#endif
    }
}
