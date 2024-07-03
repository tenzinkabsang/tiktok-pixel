namespace Nop.Plugin.Widgets.TikTokPixel.Models;

public record ConfigurationModel
{
    #region Ctor

    public ConfigurationModel()
    {
        TikTokPixelSearchModel = new TikTokPixelSearchModel();
    }

    #endregion

    #region Properties

    public bool HideList { get; set; }

    public TikTokPixelSearchModel TikTokPixelSearchModel { get; set; }

    #endregion
}
