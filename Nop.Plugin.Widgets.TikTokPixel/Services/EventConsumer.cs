using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Messages;
using Nop.Core.Domain.Orders;
using Nop.Core.Events;
using Nop.Services.Events;
using Nop.Services.Messages;
using Nop.Web.Framework.Events;
using Nop.Web.Framework.Models;
using Nop.Web.Models.Catalog;

namespace Nop.Plugin.Widgets.TikTokPixel.Services;

public class EventConsumer :
     IConsumer<CustomerRegisteredEvent>,
        IConsumer<EntityInsertedEvent<ShoppingCartItem>>,
        IConsumer<MessageTokensAddedEvent<Token>>,
        IConsumer<ModelPreparedEvent<BaseNopModel>>,
        IConsumer<OrderPlacedEvent>,
        IConsumer<PageRenderingEvent>,
        IConsumer<ProductSearchEvent>
{
    #region Fields

    private readonly TikTokPixelService _tiktokPixelService;

    #endregion

    #region Ctor

    public EventConsumer(TikTokPixelService tiktokPixelService) => _tiktokPixelService = tiktokPixelService;

    #endregion

    #region Methods

    /// <summary>
    /// Handle shopping cart item inserted event
    /// </summary>
    /// <param name="eventMessage">Event message</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task HandleEventAsync(EntityInsertedEvent<ShoppingCartItem> eventMessage)
    {
        if (eventMessage?.Entity != null)
            await _tiktokPixelService.SendAddToCartEventAsync(eventMessage.Entity);
    }

    /// <summary>
    /// Handle order placed event
    /// </summary>
    /// <param name="eventMessage">Event message</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task HandleEventAsync(OrderPlacedEvent eventMessage)
    {
        if (eventMessage?.Order != null)
            await _tiktokPixelService.SendPurchaseEventAsync(eventMessage.Order);
    }

    /// <summary>
    /// Handle product details model prepared event
    /// </summary>
    /// <param name="eventMessage">Event message</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task HandleEventAsync(ModelPreparedEvent<BaseNopModel> eventMessage)
    {
        if (eventMessage?.Model is ProductDetailsModel productDetailsModel)
            await _tiktokPixelService.SendViewContentEventAsync(productDetailsModel);
    }

    /// <summary>
    /// Handle initiate checkout event
    /// </summary>
    /// <param name="eventMessage">Event message</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task HandleEventAsync(PageRenderingEvent eventMessage)
    {
        var routeName = eventMessage.GetRouteName() ?? string.Empty;
        if (routeName == TikTokPixelDefaults.CheckoutRouteName || routeName == TikTokPixelDefaults.CheckoutOnePageRouteName)
            await _tiktokPixelService.SendInitiateCheckoutEventAsync();
    }

    /// <summary>
    /// Handle product search event
    /// </summary>
    /// <param name="eventMessage">Event message</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task HandleEventAsync(ProductSearchEvent eventMessage)
    {
        if (eventMessage?.SearchTerm != null)
            await _tiktokPixelService.SendSearchEventAsync(eventMessage.SearchTerm);
    }

    /// <summary>
    /// Handle message token added event
    /// </summary>
    /// <param name="eventMessage">Event message</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task HandleEventAsync(MessageTokensAddedEvent<Token> eventMessage)
    {
        if (eventMessage?.Message?.Name == MessageTemplateSystemNames.CONTACT_US_MESSAGE)
            await _tiktokPixelService.SendContactEventAsync();
    }

    /// <summary>
    /// Handle customer registered event
    /// </summary>
    /// <param name="eventMessage">Event message</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task HandleEventAsync(CustomerRegisteredEvent eventMessage)
    {
        if (eventMessage?.Customer != null)
            await _tiktokPixelService.SendCompleteRegistrationEventAsync();
    }

    #endregion
}
