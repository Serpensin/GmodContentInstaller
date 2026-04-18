using Newtonsoft.Json;

namespace GModContentWizard
{
    /// <summary>
    /// Represents information about content, including its URL, internal name, download size, and install size.
    /// </summary>
    public class ContentInfo
    {
        /// <summary>
        /// Gets or sets the primary URL for downloading the content.
        /// </summary>
        public string PrimaryUrl { get; set; } = null!;
        /// <summary>
        /// Gets or sets the size of the primary download in bytes.
        /// </summary>
        public long PrimaryDownloadSize { get; set; }
        /// <summary>
        /// Gets or sets the format of the primary download (zip or tar.gz).
        /// </summary>
        public string PrimaryFormat { get; set; } = null!;
        /// <summary>
        /// Gets or sets the secondary URL for downloading the content (fallback server).
        /// </summary>
        public string SecondaryUrl { get; set; } = null!;
        /// <summary>
        /// Gets or sets the size of the secondary download in bytes.
        /// </summary>
        public long SecondaryDownloadSize { get; set; }
        /// <summary>
        /// Gets or sets the format of the secondary download.
        /// </summary>
        public string SecondaryFormat { get; set; } = null!;
        /// <summary>
        /// Gets or sets the internal name of the content (used as folder name).
        /// </summary>
        public string InternalName { get; set; } = null!;
        /// <summary>
        /// Gets or sets the installed size of the content in bytes.
        /// </summary>
        public long InstallSize { get; set; }
    }
}