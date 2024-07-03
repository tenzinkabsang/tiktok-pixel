using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Widgets.TikTokPixel.Models;

/// <summary>
/// Represents a TikTok Pixel model
/// </summary>
public record TikTokPixelModel : BaseNopEntityModel
{
    #region Ctor

    public TikTokPixelModel()
    {
        AvailableStores = [];
        CustomEventSearchModel = new CustomEventSearchModel();
    }

    #endregion

    #region Properties

    [NopResourceDisplayName("Plugins.Widgets.TiktokPixel.Configuration.Fields.PixelId")]
    public string PixelId { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.TiktokPixel.Configuration.Fields.AccessToken")]
    [DataType(DataType.Password)]
    public string AccessToken { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.TiktokPixel.Configuration.Fields.DisableForUsersNotAcceptingCookieConsent")]
    public bool DisableForUsersNotAcceptingCookieConsent { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.TiktokPixel.Configuration.Fields.PixelScriptEnabled")]
    public bool PixelScriptEnabled { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.TiktokPixel.Configuration.Fields.ConversionsApiEnabled")]
    public bool ConversionsApiEnabled { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.TiktokPixel.Configuration.Fields.Store")]
    public int StoreId { get; set; }

    public string StoreName { get; set; }

    public IList<SelectListItem> AvailableStores { get; set; }

    public bool HideStoresList { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.TiktokPixel.Configuration.Fields.UseAdvancedMatching")]
    public bool UseAdvancedMatching { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.TiktokPixel.Configuration.Fields.PassUserProperties")]
    public bool PassUserProperties { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.TiktokPixel.Configuration.Fields.TrackAddToCart")]
    public bool TrackAddToCart { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.TiktokPixel.Configuration.Fields.TrackPurchase")]
    public bool TrackPurchase { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.TiktokPixel.Configuration.Fields.TrackViewContent")]
    public bool TrackViewContent { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.TiktokPixel.Configuration.Fields.TrackAddToWishlist")]
    public bool TrackAddToWishlist { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.TiktokPixel.Configuration.Fields.TrackInitiateCheckout")]
    public bool TrackInitiateCheckout { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.TiktokPixel.Configuration.Fields.TrackSearch")]
    public bool TrackSearch { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.TiktokPixel.Configuration.Fields.TrackContact")]
    public bool TrackContact { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.TiktokPixel.Configuration.Fields.TrackCompleteRegistration")]
    public bool TrackCompleteRegistration { get; set; }

    public bool HideCustomEventsSearch { get; set; }

    public CustomEventSearchModel CustomEventSearchModel { get; set; }

    #endregion
}
