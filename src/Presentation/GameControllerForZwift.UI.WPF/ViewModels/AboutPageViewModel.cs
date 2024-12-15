using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameControllerForZwift.Core;
using Markdig.Wpf;
using XamlReader = System.Windows.Markup.XamlReader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Markdown = Markdig.Markdown;
using Markdig;
using System.Xaml;
using System.Reflection;

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

        public AboutPageViewModel()
        {
            //var markdown = 
            //KoFiLogo = new BitmapImage(new Uri("pack://application:,,,/GameControllerForZwift.UI.WPF;component/Assets/ko-fi.png"));
            //KoFiLogo = new BitmapImage(new Uri("https://cdn.prod.website-files.com/5c14e387dab576fe667689cf/670f5a0172b90570b1c21dab_kofi_logo-p-500.png"));

            LoadMarkdown();
        }

        private static MarkdownPipeline BuildPipeline()
        {
            return new MarkdownPipelineBuilder()
                .UseSupportedExtensions()
                .Build();
        }
        public void LoadMarkdown()
        {
            var markdown = ReadAcknowledgementsResource();
            var xaml = Markdig.Wpf.Markdown.ToXaml(markdown, BuildPipeline());
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml)))
            {
                using (var reader = new XamlXmlReader(stream, new MyXamlSchemaContext()))
                {
                    if (XamlReader.Load(reader) is FlowDocument document)
                    {
                        Acknowledgments = document;
                    }
                }
            }
        }

        // Viewmodel should not do this.
        public string ReadAcknowledgementsResource()
        {
            //var uri = new Uri("/GameControllerForZwift.UI.WPF;component/Assets/acknowledgements.md");
            var uri = new Uri("pack://application:,,,/GameControllerForZwift.UI.WPF;component/Assets/acknowledgements.md");

            using Stream stream = Application.GetResourceStream(uri)?.Stream;
            if (stream == null)
            {
                throw new FileNotFoundException("Resource not found", "acknowledgements.md");
            }

            using StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd();
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
