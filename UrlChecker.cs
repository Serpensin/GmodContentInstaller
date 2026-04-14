#nullable enable
using System.Net.Http;

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

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Head, url);
                var response = await client.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}