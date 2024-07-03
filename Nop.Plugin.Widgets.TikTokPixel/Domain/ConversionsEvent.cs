using Newtonsoft.Json;

namespace Nop.Plugin.Widgets.TikTokPixel.Domain;

public record ConversionsEvent
{
    public ConversionsEvent()
    {
        EventSource = "web";
    }

    [JsonProperty(PropertyName = "event_source")]
    public string EventSource { get; set; }

    [JsonProperty(PropertyName = "event_source_id")]
    public string EventSourceId { get; set; }

    [JsonProperty(PropertyName = "data")]
    public List<ConversionsEventData> Data { get; set; }

    [JsonProperty(PropertyName = "test_event_code")]
    public string TestEventCode { get; set; }
}
