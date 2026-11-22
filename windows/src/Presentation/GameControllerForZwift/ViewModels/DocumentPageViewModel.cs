using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameControllerForZwift.UI.WPF.Dialogs;
using Markdig;
using Markdig.Wpf;
using System.IO;
using System.Text;
using System.Windows.Documents;
using System.Xaml;
using XamlReader = System.Windows.Markup.XamlReader;

namespace GameControllerForZwift.UI.WPF.ViewModels
{
    /// <summary>
    /// A pushed screen that shows a markdown document shipped with the app: the Credits list and the License. A link in
    /// the document opens in the default browser (SCR-CRD-011, SCR-CRD-012).
    /// </summary>
    public abstract partial class DocumentPageViewModel : ObservableObject
    {
        private readonly IDialogService _dialogs;

        protected DocumentPageViewModel(string title, string titleAutomationId, string markdown, IDialogService dialogs)
        {
            ArgumentNullException.ThrowIfNull(dialogs);
            Title = title;
            TitleAutomationId = titleAutomationId;
            _dialogs = dialogs;
            Document = ToDocument(markdown);
        }

        /// <summary>The screen title.</summary>
        public string Title { get; }

        /// <summary>The UI Automation id of the title.</summary>
        public string TitleAutomationId { get; }

        /// <summary>The document, ready to show.</summary>
        public FlowDocument? Document { get; }

        /// <summary>Opens a link of the document in the default browser. A link that is not https is ignored.</summary>
        [RelayCommand]
        private void OpenLink(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            try
            {
                _dialogs.OpenUrl(url);
            }
            catch (ArgumentException)
            {
                // Only https addresses open. Anything else is ignored.
            }
        }

        /// <summary>Reads a shipped markdown file. A missing file gives an empty document.</summary>
        protected static string ReadFile(string path) => File.Exists(path) ? File.ReadAllText(path) : string.Empty;

        private static FlowDocument? ToDocument(string markdown)
        {
            MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseSupportedExtensions().Build();
            string xaml = Markdig.Wpf.Markdown.ToXaml(markdown, pipeline);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml));
            using var reader = new XamlXmlReader(stream, new MarkdigSchemaContext());
            if (XamlReader.Load(reader) is FlowDocument document)
            {
                document.ColumnWidth = 999999;
                return document;
            }

            return null;
        }

        private sealed class MarkdigSchemaContext : XamlSchemaContext
        {
            public override bool TryGetCompatibleXamlNamespace(string xamlNamespace, out string compatibleNamespace)
            {
                if (xamlNamespace.Equals("clr-namespace:Markdig.Wpf", StringComparison.Ordinal))
                {
                    compatibleNamespace = $"clr-namespace:Markdig.Wpf;assembly={typeof(Markdig.Wpf.Styles).Assembly.FullName}";
                    return true;
                }

                return base.TryGetCompatibleXamlNamespace(xamlNamespace, out compatibleNamespace);
            }
        }
    }

    /// <summary>The Credits list: the projects and people the app builds on (SCR-CRD-003, SCR-CRD-011).</summary>
    public sealed class CreditsListViewModel : DocumentPageViewModel
    {
        public CreditsListViewModel(string markdown, IDialogService dialogs)
            : base(Localization.Localizer.Get("CreditsList"), "CreditsListPageTitle", markdown, dialogs)
        {
        }

        /// <summary>Reads the acknowledgements file that ships with the app.</summary>
        public CreditsListViewModel(IDialogService dialogs, string filePath) : this(ReadFile(filePath), dialogs)
        {
        }
    }

    /// <summary>The License screen: the full license text (SCR-CRD-004, SCR-CRD-012).</summary>
    public sealed class LicenseViewModel : DocumentPageViewModel
    {
        public LicenseViewModel(string markdown, IDialogService dialogs)
            : base(Localization.Localizer.Get("CreditsLicenseName"), "CreditsLicensePageTitle", markdown, dialogs)
        {
        }

        /// <summary>Reads the license file that ships with the app.</summary>
        public LicenseViewModel(IDialogService dialogs, string filePath) : this(ReadFile(filePath), dialogs)
        {
        }
    }
}
