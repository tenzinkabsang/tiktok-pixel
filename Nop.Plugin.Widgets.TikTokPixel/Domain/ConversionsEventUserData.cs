using Newtonsoft.Json;

namespace Nop.Plugin.Widgets.TikTokPixel.Domain;

public record ConversionsEventUserData
{
    [JsonIgnore]
    public int Id { get; set; }

    [JsonProperty(PropertyName = "ip")]
    public string ClientIpAddress { get; set; }

    [JsonProperty(PropertyName = "user_agent")]
    public string ClientUserAgent { get; set; }

    [JsonProperty(PropertyName = "email")]
    public string EmailAddress { get; set; }

    [JsonProperty(PropertyName = "phone")]
    public string PhoneNumber { get; set; }

    [JsonProperty(PropertyName = "external_id")]
    public string ExternalId { get; set; }

    [JsonProperty(PropertyName = "ttclid")]
    public string ClickId { get; set; }

    [JsonProperty(PropertyName = "ttp")]
    public string CookieId { get; set; }
}
