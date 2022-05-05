using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace CodeCaster.PVBridge.Utils.GitHub
{
    /// <summary>
    /// Checks for updates on GitHub.
    /// </summary>
    public class GitHubReleaseClient
    {
        private readonly HttpClient _httpClient;

        public GitHubReleaseClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.UserAgent.Clear();
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("PVBridge", "1.0.0"));
        }

        public async Task<Release?> GetLatestAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<Release>("https://api.github.com/repos/CodeCasterNL/PVBridge/releases/latest");
            }
            catch (Exception)
            {
                // TODO: log

                return null;
            }
        }
    }
}
