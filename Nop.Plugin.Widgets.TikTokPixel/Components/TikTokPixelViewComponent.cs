using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Nop.Plugin.Widgets.TikTokPixel.Services;
using Nop.Web.Framework.Components;
using Nop.Web.Framework.Infrastructure;

namespace Nop.Plugin.Widgets.TikTokPixel.Components;

/// <summary>
/// Represents TikTok Pixel view component
/// </summary>
public class TikTokPixelViewComponent : NopViewComponent
{
    #region Fields

    private readonly TikTokPixelService _tikTokPixelService;

    #endregion

    #region Ctor

    public TikTokPixelViewComponent(TikTokPixelService tikTokPixelService)
    {
        _tikTokPixelService = tikTokPixelService;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Invoke view component
    /// </summary>
    /// <param name="widgetZone">Widget zone name</param>
    /// <param name="additionalData">Additional data</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the view component result
    /// </returns>
    public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData)
    {
        var script = widgetZone != PublicWidgetZones.HeadHtmlTag
            ? await _tikTokPixelService.PrepareCustomEventsScriptAsync(widgetZone)
            : await _tikTokPixelService.PrepareScriptAsync();

        return new HtmlContentViewComponentResult(new HtmlString(script ?? string.Empty));
    }

    #endregion
}
