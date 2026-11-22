using GameControllerForZwift.UI.WPF;
using GameControllerForZwift.UI.WPF.Dialogs;
using GameControllerForZwift.UI.WPF.Navigation;
using GameControllerForZwift.UI.WPF.ViewModels;
using NSubstitute;

namespace GameControllerForZwift.UI.WPF.Tests.ViewModels
{
    [Collection(GameControllerForZwift.UnitTests.WpfWindows.Name)]
    public sealed class CreditsAndSupportViewModelTests
    {
        private readonly FakeDialogService _dialogs = new();
        private readonly INavigationService _navigation = Substitute.For<INavigationService>();

        [Fact]
        public void SCRCRD001_VersionLineShowsTheRealVersion()
        {
            var viewModel = new CreditsViewModel(_navigation, _dialogs);

            Assert.Equal("Version " + AppVersion.Current, viewModel.VersionText);
            Assert.DoesNotContain("+", AppVersion.Current, StringComparison.Ordinal);
        }

        [Fact]
        public void SCRCRD002_GitHubLinkOpensTheProject()
        {
            new CreditsViewModel(_navigation, _dialogs).OpenGitHubCommand.Execute(null);

            Assert.Equal([ProjectLinks.GitHub], _dialogs.OpenedUrls);
        }

        [Fact]
        public void SCRCRD003_SCRCRD004_CreditsAndLicenseRowsOpenPushedScreens()
        {
            var viewModel = new CreditsViewModel(_navigation, _dialogs);

            viewModel.OpenCreditsListCommand.Execute(null);
            viewModel.OpenLicenseCommand.Execute(null);

            _navigation.Received(1).NavigateTo<CreditsListViewModel>();
            _navigation.Received(1).NavigateTo<LicenseViewModel>();
        }

        [Fact]
        public void SCRCRD011_CreditsListShowsTheMarkdownAndOpensHttpsLinks()
        {
            GameControllerForZwift.UnitTests.TestApplication.Run(() =>
            {
                var viewModel = new CreditsListViewModel("## Libraries\n\n[SDL](https://libsdl.org)", _dialogs);

                Assert.Equal("Credits", viewModel.Title);
                Assert.Equal("CreditsListPageTitle", viewModel.TitleAutomationId);
                Assert.NotNull(viewModel.Document);

                viewModel.OpenLinkCommand.Execute("https://libsdl.org");
                Assert.Equal(["https://libsdl.org"], _dialogs.OpenedUrls);
            });
        }

        [Fact]
        public void SCRCRD011_ALinkThatIsNotHttpsIsIgnored()
        {
            GameControllerForZwift.UnitTests.TestApplication.Run(() =>
            {
                var dialogs = Substitute.For<IDialogService>();
                dialogs.When(d => d.OpenUrl("file:///c:/x")).Do(_ => throw new ArgumentException("no"));
                var viewModel = new CreditsListViewModel("text", dialogs);

                viewModel.OpenLinkCommand.Execute("file:///c:/x");
                viewModel.OpenLinkCommand.Execute("  ");
                viewModel.OpenLinkCommand.Execute(null);

                dialogs.DidNotReceive().OpenUrl("  ");
            });
        }

        [Fact]
        public void SCRCRD012_LicenseShowsTheShippedFileAndTreatsAMissingFileAsEmpty()
        {
            GameControllerForZwift.UnitTests.TestApplication.Run(() =>
            {
                var license = new LicenseViewModel(_dialogs, Path.Join(AppContext.BaseDirectory, "LICENSE.md"));
                var missing = new LicenseViewModel(_dialogs, Path.Join(AppContext.BaseDirectory, "no-such-file.md"));

                Assert.Equal("License", license.Title);
                Assert.Equal("CreditsLicensePageTitle", license.TitleAutomationId);
                Assert.NotNull(license.Document);
                Assert.True(File.Exists(Path.Join(AppContext.BaseDirectory, "LICENSE.md")));
                Assert.NotNull(missing.Document);
            });
        }

        [Fact]
        public void SCRCRD003_CreditsListReadsTheAcknowledgementsFile()
        {
            GameControllerForZwift.UnitTests.TestApplication.Run(() =>
            {
                var credits = new CreditsListViewModel(_dialogs, Path.Join(AppContext.BaseDirectory, "ACKNOWLEDGEMENTS.md"));

                Assert.NotNull(credits.Document);
            });
        }

        [Fact]
        public void SCRSUP002_SCRSUP003_SCRSUP004_LinksOpenTheRightPages()
        {
            var viewModel = new SupportViewModel(_dialogs);

            viewModel.OpenKofiCommand.Execute(null);
            viewModel.OpenStarCommand.Execute(null);
            viewModel.OpenContributingCommand.Execute(null);

            Assert.Equal([ProjectLinks.Kofi, ProjectLinks.GitHub, ProjectLinks.Contributing], _dialogs.OpenedUrls);
        }

        [Fact]
        public void SCRSUP005_TellAFriendCopiesTheLinkAndSaysSo()
        {
            var viewModel = new SupportViewModel(_dialogs);
            Assert.Equal(string.Empty, viewModel.ShareStatus);

            viewModel.CopyShareLinkCommand.Execute(null);

            Assert.Equal([ProjectLinks.GitHub], _dialogs.CopiedTexts);
            Assert.Equal("Link copied", viewModel.ShareStatus);
        }

        [Fact]
        public void SCRSUP005_TellAFriendSaysNothingWhenTheClipboardIsBusy()
        {
            _dialogs.ClipboardAvailable = false;
            var viewModel = new SupportViewModel(_dialogs);

            viewModel.CopyShareLinkCommand.Execute(null);

            Assert.Equal(string.Empty, viewModel.ShareStatus);
        }

        [Fact]
        public void SCRSUP006_ProjectLinksAreHttps()
        {
            foreach (string url in new[] { ProjectLinks.GitHub, ProjectLinks.Issues, ProjectLinks.Contributing, ProjectLinks.Kofi, ProjectLinks.LicenseText })
            {
                Assert.StartsWith("https://", url, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void DialogServiceOpensOnlyHttpsAddresses()
        {
            var dialogs = new DialogService();

            Assert.Throws<ArgumentException>(() => dialogs.OpenUrl("file:///c:/windows/system32/calc.exe"));
            Assert.Throws<ArgumentException>(() => dialogs.OpenUrl("not a url"));
        }

        [Theory]
        [InlineData("1.2.3+abc", "1.2.3")]
        [InlineData("1.2.3", "1.2.3")]
        public void AppVersion_DropsBuildMetadata(string informational, string expected)
        {
            var builder = System.Reflection.Emit.AssemblyBuilder.DefineDynamicAssembly(
                new System.Reflection.AssemblyName("VersionTest"), System.Reflection.Emit.AssemblyBuilderAccess.Run);
            builder.SetCustomAttribute(new System.Reflection.Emit.CustomAttributeBuilder(
                typeof(System.Reflection.AssemblyInformationalVersionAttribute).GetConstructor([typeof(string)])!, [informational]));
            System.Reflection.Assembly assembly = builder;

            Assert.Equal(expected, AppVersion.Read(assembly));
        }
    }
}
