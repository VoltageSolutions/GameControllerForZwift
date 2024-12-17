using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameControllerForZwift.Logic.FileSystem;
using Markdig;
using Markdig.Wpf;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Documents;
using System.Xaml;
using XamlReader = System.Windows.Markup.XamlReader;

namespace GameControllerForZwift.UI.WPF.ViewModels
{
    public partial class AboutPageViewModel : ObservableObject
    {
        #region Fields
        [ObservableProperty]
        private string _title = "About";

        [ObservableProperty]
        private string _description = "GameControllerForZwift is an open source application. Voltage Solutions provides this application under GNU GPLv3. https://www.gnu.org/licenses/gpl-3.0.en.html.";

        [ObservableProperty]
        private FlowDocument _acknowledgments;

        #endregion

        #region Constructor

        public AboutPageViewModel(string acknowledgementsContent)
        {
            LoadMarkdown(acknowledgementsContent);
        }

        public AboutPageViewModel(IFileService fileService, string filePath) : this(fileService.ReadFileContent(filePath)) { }

        private static MarkdownPipeline BuildPipeline()
        {
            return new MarkdownPipelineBuilder()
                .UseSupportedExtensions()
                .Build();
        }
        public void LoadMarkdown(string acknowledgementsContent)
        {
            var xaml = Markdig.Wpf.Markdown.ToXaml(acknowledgementsContent, BuildPipeline());
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml)))
            {
                using (var reader = new XamlXmlReader(stream, new MyXamlSchemaContext()))
                {
                    if (XamlReader.Load(reader) is FlowDocument document)
                    {
                        document.ColumnWidth = 999999;
                        Acknowledgments = document;
                    }
                }
            }
        }

        #endregion

        #region Commands

        [RelayCommand]
        public void OpenGitHub()
        {
            string url = "https://github.com/VoltageSolutions/GameControllerForZwift";
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }

        [RelayCommand]
        public void Openkofi()
        {
            string url = "https://ko-fi.com/waveguide";
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }

        #endregion

        class MyXamlSchemaContext : XamlSchemaContext
        {
            public override bool TryGetCompatibleXamlNamespace(string xamlNamespace, out string compatibleNamespace)
            {
                if (xamlNamespace.Equals("clr-namespace:Markdig.Wpf", StringComparison.Ordinal))
                {
                    compatibleNamespace = $"clr-namespace:Markdig.Wpf;assembly={Assembly.GetAssembly(typeof(Markdig.Wpf.Styles)).FullName}";
                    return true;
                }
                return base.TryGetCompatibleXamlNamespace(xamlNamespace, out compatibleNamespace);
            }
        }
    }
}
