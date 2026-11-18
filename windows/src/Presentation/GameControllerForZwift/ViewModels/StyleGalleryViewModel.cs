using CommunityToolkit.Mvvm.ComponentModel;

namespace GameControllerForZwift.UI.WPF.ViewModels
{
    /// <summary>
    /// View model of the style gallery, a Debug-only screen that shows every shared component in each state
    /// (docs/spec/ux/visual-system.md). Its controls bind here so they can be tried out.
    /// </summary>
    public sealed partial class StyleGalleryViewModel : ObservableObject
    {
        /// <summary>Choices for the sample dropdown.</summary>
        public IReadOnlyList<string> SampleChoices { get; } = ["Default", "Climbing", "Shimano style", "SRAM style"];

        [ObservableProperty]
        private string _selectedChoice = "Climbing";

        [ObservableProperty]
        private bool _isChecked = true;

        [ObservableProperty]
        private bool _isSwitchOn = true;

        [ObservableProperty]
        private string _sampleText = "Climbing";
    }
}
