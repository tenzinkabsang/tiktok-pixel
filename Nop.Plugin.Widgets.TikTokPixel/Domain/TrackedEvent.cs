namespace Nop.Plugin.Widgets.TikTokPixel.Domain;

public class TrackedEvent
{
    public string EventName { get; set; }

    public int StoreId { get; set; }

    public int CustomerId { get; set; }

    public bool IsCustomEvent { get; set; }

    public IList<string> EventObjects { get; set; } = [];
}
