using System.Collections.ObjectModel;
using System.Reflection;

namespace GameControllerForZwift.UI.WPF.Navigation
{
    public class PagesInfoDataItem
    {
        public string UniqueId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconGlyph { get; set; } = string.Empty;
        //public string ImagePath { get; set; }
        public string ViewModelName { get; set; } = string.Empty;
        public bool IsGroup { get; set; }

        public Type? ViewModelType
        {
            get
            {
                return _assembly.GetType($"GameControllerForZwift.UI.WPF.ViewModels.{ViewModelName}");
            }
        }

        //public Uri ImageSource
        //{
        //    get
        //    {
        //        return new Uri($"pack://application:,,,/{ImagePath}");
        //    }
        //}

        public ObservableCollection<PagesInfoDataItem> Items { get; set; } = new ObservableCollection<PagesInfoDataItem>();

        public override string ToString()
        {
            return Title;
        }

        private static readonly Assembly _assembly = typeof(PagesInfoDataSource).Assembly;
    }
}
