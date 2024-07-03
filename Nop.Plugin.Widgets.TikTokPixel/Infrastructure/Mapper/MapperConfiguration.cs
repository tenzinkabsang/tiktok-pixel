using AutoMapper;
using Nop.Core.Infrastructure.Mapper;
using Nop.Plugin.Widgets.TikTokPixel.Domain;
using Nop.Plugin.Widgets.TikTokPixel.Models;

namespace Nop.Plugin.Widgets.TikTokPixel.Infrastructure.Mapper;

/// <summary>
/// Represents AutoMapper configuration for plugin models
/// </summary>
public class MapperConfiguration : Profile, IOrderedMapperProfile
{
    #region Ctor

    public MapperConfiguration()
    {
        CreateMap<TikTokPixelConfiguration, TikTokPixelModel>()
            .ForMember(model => model.AvailableStores, options => options.Ignore())
            .ForMember(model => model.CustomEventSearchModel, options => options.Ignore())
            .ForMember(model => model.CustomProperties, options => options.Ignore())
            .ForMember(model => model.HideCustomEventsSearch, options => options.Ignore())
            .ForMember(model => model.HideStoresList, options => options.Ignore())
            .ForMember(model => model.StoreName, options => options.Ignore());
        CreateMap<TikTokPixelModel, TikTokPixelConfiguration>()
            .ForMember(entity => entity.CustomEvents, options => options.Ignore());
    }

    #endregion

    #region Properties

    /// <summary>
    /// Order of this mapper implementation
    /// </summary>
    public int Order => 1;

    #endregion
}
