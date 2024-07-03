using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Widgets.TikTokPixel.Models;

public record CustomEventModel : BaseNopModel
{
    #region Ctor

    public CustomEventModel()
    {
        WidgetZonesIds = [];
        WidgetZones = [];
        AvailableWidgetZones = [];
    }

    #endregion

    #region Properties

    public int ConfigurationId { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.TiktokPixel.Configuration.CustomEvents.Fields.EventName")]
    public string EventName { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.TiktokPixel.Configuration.CustomEvents.Fields.WidgetZones")]
    public IList<int> WidgetZonesIds { get; set; }

    public IList<string> WidgetZones { get; set; }

    public IList<SelectListItem> AvailableWidgetZones { get; set; }

    public string WidgetZonesName { get; set; }

    #endregion
}
