using Newtonsoft.Json;

namespace Nop.Plugin.Widgets.TikTokPixel.Domain;

public record EventPageSource
{
    [JsonProperty(PropertyName = "url")]
    public string Url { get; set; }

    [JsonProperty(PropertyName = "referrer")]
    public string Referrer { get; set; }
}
