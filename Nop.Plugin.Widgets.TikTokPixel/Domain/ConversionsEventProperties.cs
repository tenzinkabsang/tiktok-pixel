using Newtonsoft.Json;

namespace Nop.Plugin.Widgets.TikTokPixel.Domain;

public record ConversionsEventProperties
{
    /// <summary>
    /// Gets or sets a numeric value associated with this event. this could be a monetary value or a value in some other metric
    /// </summary>
    [JsonProperty(PropertyName = "value")]
    public decimal? Value { get; set; }

    /// <summary>
    /// Gets or sets the currency for the value specified, if applicable. currency must be a valid iso 4217 three-digit currency code
    /// </summary>
    [JsonProperty(PropertyName = "currency")]
    public string Currency { get; set; }

    /// <summary>
    /// Gets or sets a list of json objects that contain the product ids associated with the event plus information about the products
    /// </summary>
    [JsonProperty(PropertyName = "contents")]
    public List<ConversionEventContent> Contents { get; set; }

    /// <summary>
    /// Gets or sets a search query made by a user.
    /// </summary>
    [JsonProperty(PropertyName = "query")]
    public string Query { get; set; }

    /// <summary>
    /// Gets or sets the Order Id.
    /// </summary>
    [JsonProperty(PropertyName = "order_id")]
    public string OrderId { get; set; }
}
