namespace GameControllerForZwift.UI.WPF.Navigation
{
    /// <summary>
    /// Event arguments for the Navigating event.
    /// </summary>
    public class NavigatingEventArgs : EventArgs
    {
        public Type? PageType { get; set; }

        public NavigatingEventArgs() { }

        public NavigatingEventArgs(Type? pageType)
        {
            PageType = pageType;
        }
    }
}
