using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Widgets.TikTokPixel.Models;

public record TikTokPixelSearchModel : BaseSearchModel
{
    #region Ctor

    public TikTokPixelSearchModel()
    {
        AvailableStores = [];
    }

    #endregion

    #region Properties

    [NopResourceDisplayName("Plugins.Widgets.TiktokPixel.Configuration.Search.Store")]
    public int StoreId { get; set; }

    public IList<SelectListItem> AvailableStores { get; set; }

    public bool HideStoresList { get; set; }

    public bool HideSearchBlock { get; set; }

    #endregion
}
