using Newtonsoft.Json;

namespace GModContentWizard
{
    /// <summary>
    /// Represents a dictionary of URLs mapped to their corresponding content information.
    /// </summary>
    public class UrlDictionary
    {
        private readonly Dictionary<string, ContentInfo> _contentDictionary;

        /// <summary>
        /// Initializes a new instance of the <see cref="UrlDictionary"/> class with the specified JSON content.
        /// </summary>
        /// <param name="jsonContent">The JSON content representing the dictionary.</param>
        /// <exception cref="ArgumentException">Thrown when the JSON content is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when there is an error loading the dictionary from the JSON content.</exception>
        public UrlDictionary(string jsonContent)
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                throw new ArgumentException("JSON content cannot be null or empty.", nameof(jsonContent));
            }

            _contentDictionary = JsonConvert.DeserializeObject<Dictionary<string, ContentInfo>>(jsonContent)
                ?? throw new InvalidOperationException("Error loading dictionary from JSON content.");
        }

        /// <summary>
        /// Gets the content information by name.
        /// </summary>
        /// <param name="name">The name of the content.</param>
        /// <returns>The <see cref="ContentInfo"/> associated with the specified name, or null if not found.</returns>
        /// <exception cref="ArgumentException">Thrown when the name is null or empty.</exception>
        public ContentInfo GetContentInfoByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Name cannot be null or empty.", nameof(name));
            }

            return _contentDictionary.TryGetValue(name, out var contentInfo) ? contentInfo : null;
        }
    }

    /// <summary>
    /// Represents information about content, including its URL, internal name, download size, and install size.
    /// </summary>
    public class ContentInfo
    {
        /// <summary>
        /// Gets or sets the URL of the content.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the internal name of the content.
        /// </summary>
        public string InternalName { get; set; }

        /// <summary>
        /// Gets or sets the download size of the content in bytes.
        /// </summary>
        public long DownloadSize { get; set; }

        /// <summary>
        /// Gets or sets the install size of the content in bytes.
        /// </summary>
        public long InstallSize { get; set; }
    }
}
