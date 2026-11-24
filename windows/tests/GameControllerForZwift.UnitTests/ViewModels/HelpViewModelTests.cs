using GameControllerForZwift.UI.WPF;
using GameControllerForZwift.UI.WPF.Navigation;
using GameControllerForZwift.UI.WPF.ViewModels;
using NSubstitute;

namespace GameControllerForZwift.UI.WPF.Tests.ViewModels
{
    /// <summary>The Help and feedback screen (docs/spec/screens/help.md).</summary>
    public sealed class HelpViewModelTests : IDisposable
    {
        private readonly ConfigurationTestContext _context = new();
        private readonly INavigationService _navigation = Substitute.For<INavigationService>();

        public void Dispose() => _context.Dispose();

        private HelpViewModel Create(string? logs = null) => new(_context.Dialogs, _navigation, logs);

        [Fact]
        public void SCRHELP001_TheCommonProblemsEachHaveAPlainFix()
        {
            HelpViewModel viewModel = Create();

            Assert.NotEmpty(viewModel.Problems);
            Assert.All(viewModel.Problems, p =>
            {
                Assert.False(string.IsNullOrWhiteSpace(p.Title));
                Assert.False(string.IsNullOrWhiteSpace(p.Fix));
            });
            Assert.Contains(viewModel.Problems, p => p.Title == "Zwift can't see your controller");
        }

        [Fact]
        public void SCRHELP002_TheFindLinkOpensConnections()
        {
            Create().OpenConnectionsCommand.Execute(null);

            _navigation.Received(1).NavigateToRoot(typeof(ConnectionsViewModel));
        }

        [Fact]
        public void SCRHELP003_OpenLogFolderCreatesAndOpensTheLogFolder()
        {
            string logs = Path.Join(_context.ConfigurationsFolder, "logs-test");

            Create(logs).OpenLogsFolderCommand.Execute(null);

            Assert.True(Directory.Exists(logs));
            Assert.Equal([logs], _context.Dialogs.OpenedFolders);
        }

        [Fact]
        public void SCRHELP004_SCRHELP006_ReportAProblemAndSendFeedbackOpenGitHub()
        {
            HelpViewModel viewModel = Create();

            viewModel.ReportProblemCommand.Execute(null);
            viewModel.SendFeedbackCommand.Execute(null);

            Assert.Equal([ProjectLinks.NewIssue, ProjectLinks.Issues], _context.Dialogs.OpenedUrls);
            Assert.Equal("https://github.com/VoltageSolutions/GameControllerForZwift/issues", ProjectLinks.Issues);
        }
    }
}
