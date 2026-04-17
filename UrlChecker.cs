using System.Net.Http;
using Serilog;

namespace GModContentWizard
{
    internal static class UrlChecker
    {
        private static readonly HttpClient client = new();

        static UrlChecker()
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        }

        public static async Task<bool> IsUrlReachableAsync(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            Log.Debug("Checking URL reachability: {Url}", url);
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Head, url);
                var response = await client.SendAsync(request);
                Log.Debug("HTTP response status for {Url}: {StatusCode}", url, response.StatusCode);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var contentType = response.Content.Headers.ContentType?.MediaType;
                    Log.Debug("Content-Type for {Url}: {ContentType}", url, contentType);

                    if (contentType != null && IsValidHotlinkContentType(contentType))
                    {
                        Log.Information("URL {Url} is reachable and has valid content type", url);
                        return true;
                    }
                    else
                    {
                        Log.Warning("URL {Url} has invalid content type: {ContentType}", url, contentType ?? "null");
                    }
                }
                else
                {
                    Log.Warning("URL {Url} returned status: {StatusCode}", url, response.StatusCode);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Exception in IsUrlReachableAsync for {Url}", url);
                return false;
            }

            Log.Debug("URL {Url} is not reachable or invalid", url);
            return false;
        }

        private static bool IsValidHotlinkContentType(string contentType)
        {
            Log.Debug("Checking valid hotlink content type: {ContentType}", contentType);
            var validContentTypes = new[] {
                "application/gzip",
                "application/zip"
            };
            bool valid = validContentTypes.Contains(contentType.ToLower());
            if (!valid)
                Log.Warning("Content type {ContentType} is not valid for hotlink", contentType);
            return valid;
        }
    }
}