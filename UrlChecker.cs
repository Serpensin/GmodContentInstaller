namespace GModContentWizard
{
    public class UrlChecker
    {
        private static readonly HttpClient client = new();

        /// <summary>
        /// Checks if the given URL is reachable and has a valid hotlink content type.
        /// </summary>
        /// <param name="url">The URL to check.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the URL is reachable and has a valid hotlink content type; otherwise, false.</returns>
        public static async Task<bool> IsUrlReachableAsync(string url)
        {
            try
            {
                var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var contentType = response.Content.Headers.ContentType?.MediaType;

                    if (contentType != null && IsValidHotlinkContentType(contentType))
                    {
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                SentrySdk.CaptureException(e);
                return false;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the specified content type is a valid hotlink content type.
        /// </summary>
        /// <param name="contentType">The content type to check.</param>
        /// <returns>true if the content type is valid; otherwise, false.</returns>
        private static bool IsValidHotlinkContentType(string contentType)
        {
            var validContentTypes = new[] {
                    "application/gzip"
                };

            return validContentTypes.Contains(contentType.ToLower());
        }
    }
}
