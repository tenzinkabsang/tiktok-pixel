using Newtonsoft.Json;

namespace Nop.Plugin.Widgets.TikTokPixel.Domain;

public record ConversionsEventData
{
    public ConversionsEventData()
    {
        EventId = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Gets or sets a meta pixel standard event or custom event name
    /// </summary>
    [JsonProperty(PropertyName = "event")]
    public string EventName { get; set; }

    /// <summary>
    /// Gets or sets a map that includes additional business data about the event
    /// </summary>
    [JsonProperty(PropertyName = "properties")]
    public ConversionsEventProperties Properties { get; set; }

    /// <summary>
    /// Gets or sets a event_id used for deduplication
    /// </summary>
    [JsonProperty(PropertyName = "event_id")]
    public string EventId { get; set; }

    /// <summary>
    /// Gets or sets a unix timestamp in seconds indicating when the actual event occurred
    /// </summary>
    [JsonProperty(PropertyName = "event_time")]
    public long EventTime { get; set; }

    /// <summary>
    /// Gets or sets the browser url where the event happened. the url must begin with http:// or https:// and should match the verified domain
    /// </summary>
    [JsonProperty(PropertyName = "page")]
    public EventPageSource EventPageSource { get; set; }

    /// <summary>
    /// Gets or sets a map that contains customer information data
    /// </summary>
    [JsonProperty(PropertyName = "user")]
    public ConversionsEventUserData UserData { get; set; }

    /// <summary>
    /// Gets or sets a store identifier
    /// </summary>
    [JsonIgnore]
    public int? StoreId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the event is custom
    /// </summary>
    [JsonIgnore]
    public bool IsCustomEvent { get; set; }
}
