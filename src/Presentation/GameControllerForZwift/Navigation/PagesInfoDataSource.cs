using System.Collections.ObjectModel;
using System.Text.Json;

namespace GameControllerForZwift.UI.WPF.Navigation
{
    public sealed class PagesInfoDataSource
    {
        private static readonly object _lock = new();

        #region Singleton

        private static readonly PagesInfoDataSource _instance;

        public static PagesInfoDataSource Instance
        {
            get
            {
                return _instance;
            }
        }

        static PagesInfoDataSource()
        {
            _instance = new PagesInfoDataSource();
        }

        private PagesInfoDataSource()
        {
            var jsonText = ReadPagesData();
            PagesInfo = JsonSerializer.Deserialize<List<PagesInfoDataItem>>(jsonText) ?? new List<PagesInfoDataItem>();
        }

        #endregion

        public ICollection<PagesInfoDataItem> PagesInfo { get; }

        private static string ReadPagesData()
        {
            var assembly = typeof(PagesInfoDataSource).Assembly;
            var resourceName = "GameControllerForZwift.Navigation.PagesInfoData.json";

            using (var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' was not found."))
            using (var reader = new System.IO.StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        public ICollection<PagesInfoDataItem>? GetControlsInfo(string groupName)
        {
            return PagesInfo.Where(x => x.UniqueId == groupName).FirstOrDefault()?.Items;
        }

        public ICollection<PagesInfoDataItem> GetAllPagesnfo()
        {
            var allPages = new ObservableCollection<PagesInfoDataItem>();
            foreach (PagesInfoDataItem pagesInfoDataItem in PagesInfo)
            {
                var items = pagesInfoDataItem.Items;
                foreach (PagesInfoDataItem item in items)
                {
                    allPages.Add(item);
                }

            }

            return allPages;
        }

        public ICollection<PagesInfoDataItem> GetGroupedPagesInfo()
        {
            return PagesInfo.Where(x => x.IsGroup == true).ToList();
        }
    }
}
