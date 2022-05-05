using System.Text.Json.Serialization;

namespace CodeCaster.PVBridge.Utils.GitHub
{
    public class Release
    {
        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }

        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }
        
        [JsonPropertyName("published_at")]
        public DateTime? PublishedAt { get; set; }
        
        [JsonPropertyName("body")]
        public string? Body { get; set; }
    }
}
