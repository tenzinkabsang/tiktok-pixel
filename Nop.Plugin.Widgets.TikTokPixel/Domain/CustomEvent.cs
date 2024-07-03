namespace Nop.Plugin.Widgets.TikTokPixel.Domain;

/// <summary>
/// Represents custom event configuration
/// </summary>
public class CustomEvent
{
    /// <summary>
    /// Gets or sets the custom event name
    /// </summary>
    public string EventName { get; set; }

    /// <summary>
    /// Gets or sets the list of widget zones in which this event is tracked
    /// </summary>
    public IList<string> WidgetZones { get; set; } = new List<string>();
}
