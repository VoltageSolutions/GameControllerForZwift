using System.Collections.ObjectModel;
using System.Reflection;

namespace GameControllerForZwift.UI.WPF.Navigation
{
    public class PagesInfoDataItem
    {
        public string UniqueId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string IconGlyph { get; set; }
        //public string ImagePath { get; set; }
        public string PageName { get; set; }
        public bool IsGroup { get; set; } = false;

        public Type PageType
        {
            get
            {
                return _assembly.GetType($"GameControllerForZwift.UI.WPF.Views.{PageName}");
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

        private static Assembly _assembly = typeof(PagesInfoDataSource).Assembly;
    }
}
