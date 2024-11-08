using Newtonsoft.Json;

namespace GModContentInstaller
{
    public class UrlDictionary
    {
        private readonly Dictionary<string, ContentInfo> _contentDictionary;

        public UrlDictionary(string jsonContent)
        {
            _contentDictionary = JsonConvert.DeserializeObject<Dictionary<string, ContentInfo>>(jsonContent)
                ?? throw new InvalidOperationException("Fehler beim Laden des Dictionaries.");
        }

        public ContentInfo GetContentInfoByName(string name)
        {
            return _contentDictionary.TryGetValue(name, out ContentInfo contentInfo) ? contentInfo : null;
        }
    }

    public class ContentInfo
    {
        public string Url { get; set; }
        public string InternalName { get; set; }
        public float DownloadSize { get; set; }  // in MB
        public float InstallSize { get; set; }   // in MB
    }
}
