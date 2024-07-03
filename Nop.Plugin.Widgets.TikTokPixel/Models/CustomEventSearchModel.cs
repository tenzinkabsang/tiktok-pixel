using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Widgets.TikTokPixel.Models;

public record CustomEventSearchModel : BaseSearchModel
{
    #region Ctor

    public CustomEventSearchModel()
    {
        AddCustomEvent = new CustomEventModel();
    }

    #endregion

    #region Properties

    public int ConfigurationId { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.TiktokPixel.Configuration.CustomEvents.Search.WidgetZone")]
    public string WidgetZone { get; set; }

    public CustomEventModel AddCustomEvent { get; set; }

    #endregion
}
