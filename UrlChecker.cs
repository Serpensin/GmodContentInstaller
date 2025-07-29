using SerpentModding;

namespace GModContentWizard
{
    public static class UrlChecker
    {
        private static readonly HttpClient client = new();

        /// <summary>
        /// Checks if the given URL is reachable and has a valid hotlink content type.
        /// </summary>
        /// <param name="url">The URL to check.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the URL is reachable and has a valid hotlink content type; otherwise, false.</returns>
        public static async Task<bool> IsUrlReachableAsync(string url)
        {
            Logger.Instance.Trace($"IsUrlReachableAsync called for url: {url}");
            try
            {
                var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
                Logger.Instance.Debug($"HTTP response status for {url}: {response.StatusCode}");
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var contentType = response.Content.Headers.ContentType?.MediaType;
                    Logger.Instance.Info($"Content-Type for {url}: {contentType}");
                    if (contentType != null && IsValidHotlinkContentType(contentType))
                    {
                        Logger.Instance.Info($"URL {url} is reachable and has valid content type");
                        return true;
                    }
                    else
                    {
                        Logger.Instance.Warn($"URL {url} has invalid content type: {contentType}");
                    }
                }
                else
                {
                    Logger.Instance.Warn($"URL {url} returned status: {response.StatusCode}");
                }
            }
            catch (Exception e)
            {
                Logger.Instance.Error($"Exception in IsUrlReachableAsync for {url}: {e.Message}");
                return false;
            }

            Logger.Instance.Debug($"URL {url} is not reachable or invalid");
            return false;
        }

        /// <summary>
        /// Determines whether the specified content type is a valid hotlink content type.
        /// </summary>
        /// <param name="contentType">The content type to check.</param>
        /// <returns>true if the content type is valid; otherwise, false.</returns>
        private static bool IsValidHotlinkContentType(string contentType)
        {
            Logger.Instance.Trace($"IsValidHotlinkContentType called for contentType: {contentType}");
            var validContentTypes = new[] {
                "application/gzip",
                "application/zip"
            };
            bool valid = validContentTypes.Contains(contentType.ToLower());
            if (!valid)
                Logger.Instance.Warn($"Content type {contentType} is not valid for hotlink");
            return valid;
        }
    }
}
