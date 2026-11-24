using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameControllerForZwift.UI.WPF.Dialogs;
using GameControllerForZwift.UI.WPF.Localization;
using GameControllerForZwift.UI.WPF.Navigation;
using System.IO;

namespace GameControllerForZwift.UI.WPF.ViewModels
{
    /// <summary>One common problem on the Help and feedback screen, with its short plain fix (SCR-HELP-001).</summary>
    /// <param name="Title">The problem, in the words a rider would use.</param>
    /// <param name="Fix">What to do about it.</param>
    public sealed record HelpProblem(string Title, string Fix);

    /// <summary>View model of the Help and feedback screen (docs/spec/screens/help.md).</summary>
    public sealed partial class HelpViewModel : ObservableObject
    {
        private readonly IDialogService _dialogs;
        private readonly INavigationService _navigation;
        private readonly string _logsFolder;

        public HelpViewModel(IDialogService dialogService, INavigationService navigationService, string? logsFolder = null)
        {
            ArgumentNullException.ThrowIfNull(dialogService);
            ArgumentNullException.ThrowIfNull(navigationService);
            _dialogs = dialogService;
            _navigation = navigationService;
            _logsFolder = logsFolder ?? Path.Join(AppContext.BaseDirectory, "logs");

            Problems =
            [
                new(Localizer.Get("HelpProblemSeeTitle"), Localizer.Get("HelpProblemSeeFix")),
                new(Localizer.Get("HelpProblemButtonTitle"), Localizer.Get("HelpProblemButtonFix")),
                new(Localizer.Get("HelpProblemMissingTitle"), Localizer.Get("HelpProblemMissingFix")),
            ];
        }

        /// <summary>The common problems that are answered on this screen, in order (SCR-HELP-001).</summary>
        public IReadOnlyList<HelpProblem> Problems { get; }

        /// <summary>Opens the Connections tab, where the steps for "Zwift can't find Voltage Gamepad" are (SCR-HELP-002).</summary>
        [RelayCommand]
        private void OpenConnections() => _navigation.NavigateToRoot(typeof(ConnectionsViewModel));

        /// <summary>Opens the log folder, creating it first (SCR-HELP-003).</summary>
        [RelayCommand]
        private void OpenLogsFolder()
        {
            try
            {
                Directory.CreateDirectory(_logsFolder);
                _dialogs.OpenFolder(_logsFolder);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _dialogs.ShowError(Localizer.Get("SettingsOpenFolderErrorTitle"), ex.Message);
            }
        }

        /// <summary>Opens GitHub to start an issue. The row says what to attach (SCR-HELP-004).</summary>
        [RelayCommand]
        private void ReportProblem() => _dialogs.OpenUrl(ProjectLinks.NewIssue);

        /// <summary>Opens the Issues page of the project on GitHub (SCR-HELP-006).</summary>
        [RelayCommand]
        private void SendFeedback() => _dialogs.OpenUrl(ProjectLinks.Issues);
    }
}
