namespace GModContentInstaller
{
    public class UrlChecker
    {
        private static readonly HttpClient client = new HttpClient();

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

        private static bool IsValidHotlinkContentType(string contentType)
        {
            var validContentTypes = new[] {
                "application/zip",
                "application/x-7z-compressed"
            };

            return validContentTypes.Contains(contentType.ToLower());
        }
    }
}
