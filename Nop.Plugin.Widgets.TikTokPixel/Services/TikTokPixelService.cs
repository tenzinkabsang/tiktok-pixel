using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Http.Extensions;
using Nop.Data;
using Nop.Plugin.Widgets.TikTokPixel.Domain;
using Nop.Services.Catalog;
using Nop.Services.Cms;
using Nop.Services.Common;
using Nop.Services.Directory;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Tax;
using Nop.Web.Framework.Models.Cms;
using Nop.Web.Models.Catalog;

namespace Nop.Plugin.Widgets.TikTokPixel.Services;

/// <summary>
/// Represents TikTok Pixel service
/// </summary>
public class TikTokPixelService
{
    #region Constants

    protected const int TABS_NUMBER = 2;

    #endregion

    #region Fields

    protected readonly CurrencySettings _currencySettings;
    protected readonly ICategoryService _categoryService;
    protected readonly ICurrencyService _currencyService;
    protected readonly IGenericAttributeService _genericAttributeService;
    protected readonly IHttpContextAccessor _httpContextAccessor;
    protected readonly ILogger _logger;
    protected readonly IOrderService _orderService;
    protected readonly IOrderTotalCalculationService _orderTotalCalculationService;
    protected readonly IPriceCalculationService _priceCalculationService;
    protected readonly IProductService _productService;
    protected readonly IRepository<TikTokPixelConfiguration> _tiktokPixelConfigurationRepository;
    protected readonly IShoppingCartService _shoppingCartService;
    protected readonly IStaticCacheManager _staticCacheManager;
    protected readonly IStoreContext _storeContext;
    protected readonly ITaxService _taxService;
    protected readonly IWebHelper _webHelper;
    protected readonly IWidgetPluginManager _widgetPluginManager;
    protected readonly IWorkContext _workContext;
    protected readonly TikTokConversionsHttpClient _tiktokConversionsHttpClient;

    #endregion

    #region Ctor

    public TikTokPixelService(CurrencySettings currencySettings,
        ICategoryService categoryService,
        ICurrencyService currencyService,
        IGenericAttributeService genericAttributeService,
        IHttpContextAccessor httpContextAccessor,
        ILogger logger,
        IOrderService orderService,
        IOrderTotalCalculationService orderTotalCalculationService,
        IPriceCalculationService priceCalculationService,
        IProductService productService,
        IRepository<TikTokPixelConfiguration> tiktokPixelConfigurationRepository,
        IShoppingCartService shoppingCartService,
        IStaticCacheManager staticCacheManager,
        IStoreContext storeContext,
        ITaxService taxService,
        TikTokConversionsHttpClient tiktokConversionsHttpClient,
        IWebHelper webHelper,
        IWidgetPluginManager widgetPluginManager,
        IWorkContext workContext)
    {
        _currencySettings = currencySettings;
        _categoryService = categoryService;
        _currencyService = currencyService;
        _genericAttributeService = genericAttributeService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _orderService = orderService;
        _orderTotalCalculationService = orderTotalCalculationService;
        _priceCalculationService = priceCalculationService;
        _productService = productService;
        _tiktokPixelConfigurationRepository = tiktokPixelConfigurationRepository;
        _shoppingCartService = shoppingCartService;
        _staticCacheManager = staticCacheManager;
        _storeContext = storeContext;
        _taxService = taxService;
        _tiktokConversionsHttpClient = tiktokConversionsHttpClient;
        _webHelper = webHelper;
        _widgetPluginManager = widgetPluginManager;
        _workContext = workContext;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Handle function and get result
    /// </summary>
    /// <typeparam name="TResult">Result type</typeparam>
    /// <param name="function">Function</param>
    /// <param name="logErrors">Whether to log errors</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the function result
    /// </returns>
    protected async Task<TResult> HandleFunctionAsync<TResult>(Func<Task<TResult>> function, bool logErrors = true)
    {
        try
        {
            //check whether the plugin is active
            if (!await PluginActiveAsync())
                return default;

            //invoke function
            return await function();
        }
        catch (Exception exception)
        {
            if (!logErrors)
                return default;

            var customer = await _workContext.GetCurrentCustomerAsync();
            if (customer.IsSearchEngineAccount() || customer.IsBackgroundTaskAccount())
                return default;

            //log errors
            var error = $"{TikTokPixelDefaults.SystemName} error: {Environment.NewLine}{exception.Message}";
            await _logger.ErrorAsync(error, exception, customer);

            return default;
        }
    }

    /// <summary>
    /// Check whether the plugin is active for the current user and the current store
    /// </summary>
    /// /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the result
    /// </returns>
    protected async Task<bool> PluginActiveAsync()
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();
        return await _widgetPluginManager.IsPluginActiveAsync(TikTokPixelDefaults.SystemName, customer, store.Id);
    }

    protected async Task<List<ConversionsEventData>> PrepareAddToCartEventModelAsync(ShoppingCartItem item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        //check whether the shopping was initiated by the customer
        var customer = await _workContext.GetCurrentCustomerAsync();

        var store = await _storeContext.GetCurrentStoreAsync();

        if (item.CustomerId != customer.Id)
            throw new NopException("Shopping was not initiated by customer");

        var eventName = item.ShoppingCartTypeId == (int)ShoppingCartType.ShoppingCart
            ? TikTokPixelDefaults.ADD_TO_CART
            : TikTokPixelDefaults.ADD_TO_WISHLIST;

        var product = await _productService.GetProductByIdAsync(item.ProductId);
        var categoryMapping = (await _categoryService.GetProductCategoriesByProductIdAsync(product?.Id ?? 0)).FirstOrDefault();
        var categoryName = (await _categoryService.GetCategoryByIdAsync(categoryMapping?.CategoryId ?? 0))?.Name;
        var sku = product != null ? await _productService.FormatSkuAsync(product, item.AttributesXml) : string.Empty;
        var quantity = product != null ? (int?)item.Quantity : null;
        var (productPrice, _, _, _) = await _priceCalculationService.GetFinalPriceAsync(product, customer, store, includeDiscounts: false);
        var (price, _) = await _taxService.GetProductPriceAsync(product, productPrice);
        var currentCurrency = await _workContext.GetWorkingCurrencyAsync();
        var priceValue = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(price, currentCurrency);
        var currency = currentCurrency?.CurrencyCode;

        var properties = new ConversionsEventProperties
        {
            Value = priceValue,
            Currency = currency,
            Contents = new List<ConversionEventContent>
                {
                    new()
                    {
                        ContentId = sku,
                        ContentName = product.Name,
                        ContentCategory = categoryName,
                        Quantity = quantity,
                        Price = priceValue,
                        ContentType = "product"
                    }
                }
        };

        return new List<ConversionsEventData>
                {
                    new()
                    {
                        EventName = eventName,
                        EventTime = new DateTimeOffset(item.CreatedOnUtc).ToUnixTimeSeconds(),
                        EventPageSource = new EventPageSource { Url = _webHelper.GetThisPageUrl(true) },
                        UserData = await PrepareUserDataAsync(customer),
                        Properties = properties,
                        StoreId = item.StoreId
                    }
                };
    }

    /// <summary>
    /// Prepare purchase event model
    /// </summary>
    /// <param name="order">Order</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the ConversionsEvent model
    /// </returns>
    protected async Task<List<ConversionsEventData>> PreparePurchaseModelAsync(Order order)
    {
        if (order == null)
            throw new ArgumentNullException(nameof(order));

        //check whether the purchase was initiated by the customer
        var customer = await _workContext.GetCurrentCustomerAsync();
        if (order.CustomerId != customer.Id)
            throw new NopException("Purchase was not initiated by customer");

        var store = await _storeContext.GetCurrentStoreAsync();
        var currentCurrency = await _workContext.GetWorkingCurrencyAsync();

        //prepare event object
        var currency = await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId);
        var contents = await (await _orderService.GetOrderItemsAsync(order.Id)).SelectAwait(async item =>
        {
            var product = await _productService.GetProductByIdAsync(item.ProductId);
            var sku = product != null ? await _productService.FormatSkuAsync(product, item.AttributesXml) : string.Empty;
            var quantity = product != null ? (int?)item.Quantity : null;

            var categoryMapping = (await _categoryService.GetProductCategoriesByProductIdAsync(product?.Id ?? 0)).FirstOrDefault();
            var categoryName = (await _categoryService.GetCategoryByIdAsync(categoryMapping?.CategoryId ?? 0))?.Name;

            var (productPrice, _, _, _) = await _priceCalculationService.GetFinalPriceAsync(product, customer, store, includeDiscounts: false);
            var (price, _) = await _taxService.GetProductPriceAsync(product, productPrice);
            var priceValue = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(price, currentCurrency);

            return new ConversionEventContent
            {
                ContentId = sku,
                ContentName = product.Name,
                Quantity = quantity,
                Price = priceValue,
                ContentCategory = categoryName,
                ContentType = "product"
            };
        }).ToListAsync();

        return new List<ConversionsEventData>
                {
                    new()
                    {
                        EventName = TikTokPixelDefaults.PURCHASE,
                        EventTime = new DateTimeOffset(order.CreatedOnUtc).ToUnixTimeSeconds(),
                        EventPageSource = new EventPageSource { Url = _webHelper.GetThisPageUrl(true) },
                        UserData = await PrepareUserDataAsync(customer),
                        Properties = new ConversionsEventProperties { Value = order.OrderTotal, Currency = currency?.CurrencyCode, Contents = contents },
                        StoreId = order.StoreId
                    }
                };
    }

    /// <summary>
    /// Prepare view content event model
    /// </summary>
    /// <param name="productDetails">Product details model</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the ConversionsEvent model
    /// </returns>
    protected async Task<List<ConversionsEventData>> PrepareViewContentModelAsync(ProductDetailsModel productDetails)
    {
        if (productDetails == null)
            throw new ArgumentNullException(nameof(productDetails));

        //prepare event object
        var store = await _storeContext.GetCurrentStoreAsync();
        var product = await _productService.GetProductByIdAsync(productDetails.Id);
        var categoryMapping = (await _categoryService.GetProductCategoriesByProductIdAsync(product?.Id ?? 0)).FirstOrDefault();
        var categoryName = (await _categoryService.GetCategoryByIdAsync(categoryMapping?.CategoryId ?? 0))?.Name;
        var sku = productDetails.Sku;
        var priceValue = productDetails.ProductPrice.PriceValue;
        var currency = (await _workContext.GetWorkingCurrencyAsync())?.CurrencyCode;
        var pageUrl = _webHelper.GetThisPageUrl(true);
        var properties = new ConversionsEventProperties
        {
            Value = priceValue,
            Currency = currency,
            Contents = new List<ConversionEventContent>
                {
                    new()
                    {
                        ContentId = sku,
                        ContentName = product.Name,
                        ContentCategory = categoryName,
                        Price = priceValue,
                        ContentType = "product",

                        // Additional fields for Product feed
                        Description = (product.ShortDescription ?? string.Empty).ReplaceLineEndings(" "),
                        Availability = productDetails.InStock ? "in stock" : "out of stock",
                        Condition = "new",
                        Brand = store.CompanyName,
                        ProductType = categoryName,
                        ProductLink = pageUrl,
                        ImageLink = productDetails.DefaultPictureModel.FullSizeImageUrl
                    }
                }
        };

        return new List<ConversionsEventData>
                {
                    new()
                    {
                        EventName = TikTokPixelDefaults.VIEW_CONTENT,
                        EventTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
                        EventPageSource = new EventPageSource { Url = pageUrl },
                        UserData = await PrepareUserDataAsync(),
                        Properties = properties
                    }
                };
    }

    /// <summary>
    /// Prepare initiate checkout event model
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the ConversionsEvent model
    /// </returns>
    protected async Task<List<ConversionsEventData>> PrepareInitiateCheckoutModelAsync()
    {
        //prepare event object
        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
        var (price, _, _, _, _, _) = await _orderTotalCalculationService.GetShoppingCartTotalAsync(cart, false, false);
        var currentCurrency = await _workContext.GetWorkingCurrencyAsync();
        var priceValue = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(price ?? 0, currentCurrency);
        var currency = currentCurrency?.CurrencyCode;

        var contentsProperties = await cart.SelectAwait(async item =>
        {
            var product = await _productService.GetProductByIdAsync(item.ProductId);
            var sku = product != null ? await _productService.FormatSkuAsync(product, item.AttributesXml) : string.Empty;
            var quantity = product != null ? (int?)item.Quantity : null;
            return new ConversionEventContent
            {
                ContentId = sku,
                ContentName = product.Name,
                Quantity = quantity,
                ContentType = "product"
            };
        }).ToListAsync();

        return new List<ConversionsEventData>
                {
                    new()
                    {
                        EventName = TikTokPixelDefaults.INITIATE_CHECKOUT,
                        EventTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
                        EventPageSource = new EventPageSource { Url = _webHelper.GetThisPageUrl(true) },
                        UserData = await PrepareUserDataAsync(customer),
                        Properties = new ConversionsEventProperties { Value = priceValue, Currency = currency, Contents = contentsProperties }
                    }
            };
    }

    /// <summary>
    /// Prepare search event model
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the ConversionsEvent model
    /// </returns>
    protected async Task<List<ConversionsEventData>> PrepareSearchModelAsync(string searchTerm)
    {
        return new List<ConversionsEventData>
                {
                    new()
                    {
                        EventName = TikTokPixelDefaults.SEARCH,
                        EventTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
                        EventPageSource = new EventPageSource { Url = _webHelper.GetThisPageUrl(true) },
                        UserData = await PrepareUserDataAsync(),
                        Properties = new ConversionsEventProperties { Query = JavaScriptEncoder.Default.Encode(searchTerm) }
                    }
                };
    }

    /// <summary>
    /// Prepare contact event model
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the ConversionsEvent model
    /// </returns>
    protected async Task<List<ConversionsEventData>> PrepareContactModelAsync()
    {
        return new List<ConversionsEventData>
                {
                    new()
                    {
                        EventName = TikTokPixelDefaults.CONTACT,
                        EventTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
                        EventPageSource = new EventPageSource { Url = _webHelper.GetThisPageUrl(true) },
                        UserData = await PrepareUserDataAsync(),
                        Properties = new ConversionsEventProperties()
                    }
                };
    }

    /// <summary>
    /// Prepare complete registration event model
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the ConversionsEvent model
    /// </returns>
    protected async Task<List<ConversionsEventData>> PrepareCompleteRegistrationModelAsync()
    {
        return new List<ConversionsEventData>
                {
                    new()
                    {
                        EventName = TikTokPixelDefaults.COMPLETE_REGISTRATION,
                        EventTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
                        EventPageSource = new EventPageSource { Url = _webHelper.GetThisPageUrl(true) },
                        UserData = await PrepareUserDataAsync(),
                        Properties = new ConversionsEventProperties()
                    }
                };
    }

    /// <summary>
    /// Prepare pixel script to track events
    /// </summary>
    /// <param name="conversionsEvents">Conversions event</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task PreparePixelScriptAsync(List<ConversionsEventData> conversionsEvents)
    {
        await HandleFunctionAsync(async () =>
        {
            var events = (await _httpContextAccessor.HttpContext.Session.GetAsync<IList<TrackedEvent>>(TikTokPixelDefaults.TrackedEventsSessionValue)) ?? [];
            foreach (var conversionsEventData in conversionsEvents)
            {
                conversionsEventData.StoreId ??= (await _storeContext.GetCurrentStoreAsync()).Id;
                var activeEvent = events.FirstOrDefault(trackedEvent =>
                    trackedEvent.EventName == conversionsEventData.EventName && trackedEvent.CustomerId == conversionsEventData.UserData?.Id && trackedEvent.StoreId == conversionsEventData.StoreId);
                if (activeEvent == null)
                {
                    activeEvent = new TrackedEvent
                    {
                        EventName = conversionsEventData.EventName,
                        CustomerId = conversionsEventData.UserData?.Id ?? 0,
                        StoreId = conversionsEventData.StoreId ?? 0,
                        IsCustomEvent = conversionsEventData.IsCustomEvent
                    };
                    events.Add(activeEvent);
                }
                var data = FormatCustomData(conversionsEventData.Properties);
                var eventId = FormatEventObject(new List<(string Name, object Value)> { ("event_id", conversionsEventData.EventId) });

                activeEvent.EventObjects.Add($"{data}, {eventId}");
                await _httpContextAccessor.HttpContext.Session.SetAsync(TikTokPixelDefaults.TrackedEventsSessionValue, events);
            }
            return Task.FromResult(true);
        });
    }

    /// <summary>
    /// Prepare script to track event and store it for the further using
    /// </summary>
    /// <param name="eventName">Event name</param>
    /// <param name="eventObject">Event object</param>
    /// <param name="customerId">Customer identifier</param>
    /// <param name="storeId">Store identifier</param>
    /// <param name="isCustomEvent">Whether the event is a custom one</param>
    protected async Task PrepareTrackedEventScriptAsync(string eventName, string eventObject,
        int? customerId = null, int? storeId = null, bool isCustomEvent = false)
    {
        //prepare script and store it into the session data, we use this later
        var customer = await _workContext.GetCurrentCustomerAsync();
        customerId ??= customer.Id;
        var store = await _storeContext.GetCurrentStoreAsync();
        storeId ??= store.Id;
        var events = (await _httpContextAccessor.HttpContext.Session.GetAsync<IList<TrackedEvent>>(TikTokPixelDefaults.TrackedEventsSessionValue)) ?? [];
        var activeEvent = events.FirstOrDefault(trackedEvent =>
            trackedEvent.EventName == eventName && trackedEvent.CustomerId == customerId && trackedEvent.StoreId == storeId);
        if (activeEvent == null)
        {
            activeEvent = new TrackedEvent
            {
                EventName = eventName,
                CustomerId = customerId.Value,
                StoreId = storeId.Value,
                IsCustomEvent = isCustomEvent
            };
            events.Add(activeEvent);
        }
        activeEvent.EventObjects.Add(eventObject);
        await _httpContextAccessor.HttpContext.Session.SetAsync(TikTokPixelDefaults.TrackedEventsSessionValue, events);
    }

    /// <summary>
    /// Prepare scripts
    /// </summary>
    /// <param name="configurations">Enabled configurations</param>
    protected async Task<string> PreparePixelEventCodeAsync(IList<TikTokPixelConfiguration> configurations)
    {
        return await PrepareInitScriptAsync(configurations) +
            await PrepareTrackedEventsScriptAsync(configurations);
    }

    /// <summary>
    /// Prepare user info (used with Advanced Matching feature)
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the user info
    /// </returns>
    protected async Task<string> GetUserObjectAsync()
    {
        var userData = await PrepareUserDataAsync();

        return FormatEventObject(new List<(string Name, object Value)>
            {
                ("email", userData.EmailAddress),
                ("phone_number", userData.PhoneNumber),
                ("external_id", userData.ExternalId),
                ("ip", userData.ClientIpAddress),
                ("ttclid", userData.ClickId),
                ("ttp", userData.CookieId),
                ("user_agent", userData.ClientUserAgent)
            });
    }

    protected async Task<ConversionsEventUserData> PrepareUserDataAsync(Customer customer = null)
    {
        //prepare user object
        customer ??= await _workContext.GetCurrentCustomerAsync();
        var ipAddress = _webHelper.GetCurrentIpAddress();
        var request = _httpContextAccessor.HttpContext?.Request;
        var userAgent = request?.Headers[HeaderNames.UserAgent].ToString();

        string clickId = null;
        string cookieId = null;
        request?.Cookies.TryGetValue("ttclid", out clickId);
        request?.Cookies.TryGetValue("_ttp", out cookieId);

        return new ConversionsEventUserData
        {
            Id = customer.Id,
            EmailAddress = HashHelper.CreateHash(Encoding.UTF8.GetBytes(customer.Email?.ToLowerInvariant() ?? string.Empty), "SHA256"),
            PhoneNumber = string.IsNullOrEmpty(customer.Phone) ? null : HashHelper.CreateHash(Encoding.UTF8.GetBytes($"+{new string(customer.Phone.Where(c => char.IsDigit(c)).ToArray())}"), "SHA256"),
            ExternalId = HashHelper.CreateHash(Encoding.UTF8.GetBytes(customer?.CustomerGuid.ToString()?.ToLowerInvariant() ?? string.Empty), "SHA256"),
            ClientIpAddress = ipAddress?.ToLowerInvariant(),
            ClientUserAgent = userAgent?.ToLowerInvariant(),
            ClickId = clickId,
            CookieId = cookieId
        };
    }


    /// <summary>
    /// Prepare scripts to track events
    /// </summary>
    /// <param name="configurations">Enabled configurations</param>
    protected async Task<string> PrepareTrackedEventsScriptAsync(IList<TikTokPixelConfiguration> configurations)
    {
        //get previously stored events and remove them from the session data
        var events = (await _httpContextAccessor.HttpContext.Session.GetAsync<IList<TrackedEvent>>(TikTokPixelDefaults.TrackedEventsSessionValue)) ?? [];
        var store = await _storeContext.GetCurrentStoreAsync();
        var customer = await _workContext.GetCurrentCustomerAsync();
        var activeEvents = events.Where(trackedEvent =>
            trackedEvent.CustomerId == customer.Id && trackedEvent.StoreId == store.Id)
            .ToList();
        await _httpContextAccessor.HttpContext.Session.SetAsync(TikTokPixelDefaults.TrackedEventsSessionValue, events.Except(activeEvents).ToList());

        if (!activeEvents.Any())
            return string.Empty;

        return await activeEvents.AggregateAwaitAsync(string.Empty, async (preparedScripts, trackedEvent) =>
        {
            //filter active configurations
            var activeConfigurations = trackedEvent.EventName switch
            {
                TikTokPixelDefaults.ADD_TO_CART => configurations.Where(configuration => configuration.TrackAddToCart).ToList(),
                TikTokPixelDefaults.PURCHASE => configurations.Where(configuration => configuration.TrackPurchase).ToList(),
                TikTokPixelDefaults.VIEW_CONTENT => configurations.Where(configuration => configuration.TrackViewContent).ToList(),
                TikTokPixelDefaults.ADD_TO_WISHLIST => configurations.Where(configuration => configuration.TrackAddToWishlist).ToList(),
                TikTokPixelDefaults.INITIATE_CHECKOUT => configurations.Where(configuration => configuration.TrackInitiateCheckout).ToList(),
                TikTokPixelDefaults.SEARCH => configurations.Where(configuration => configuration.TrackSearch).ToList(),
                TikTokPixelDefaults.CONTACT => configurations.Where(configuration => configuration.TrackContact).ToList(),
                TikTokPixelDefaults.COMPLETE_REGISTRATION => configurations.Where(configuration => configuration.TrackCompleteRegistration).ToList(),
                _ => new List<TikTokPixelConfiguration>()
            };
            if (trackedEvent.IsCustomEvent)
                activeConfigurations = await configurations.WhereAwait(async configuration =>
                    (await GetCustomEventsAsync(configuration.Id)).Any(customEvent => customEvent.EventName == trackedEvent.EventName)).ToListAsync();

            //prepare event scripts
            return preparedScripts + await trackedEvent.EventObjects.AggregateAwaitAsync(string.Empty, async (preparedEventScripts, eventObject) =>
            {
                return preparedEventScripts + await FormatScriptAsync(activeConfigurations, configuration =>
                {
                    var eventObjectParameter = !string.IsNullOrEmpty(eventObject) ? $", {eventObject}" : null;
                    return Task.FromResult($"ttq.instance('{configuration.PixelId}').track('{trackedEvent.EventName}'{eventObjectParameter});");
                });
            });
        });
    }

    /// <summary>
    /// Format script to look pretty
    /// </summary>
    /// <param name="configurations">Enabled configurations</param>
    /// <param name="getScript">Function to get script for the passed configuration</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the script code
    /// </returns>
    protected async Task<string> FormatScriptAsync(IList<TikTokPixelConfiguration> configurations, Func<TikTokPixelConfiguration, Task<string>> getScript)
    {
        if (!configurations.Any())
            return string.Empty;

        //format script
        var formattedScript = await configurations.AggregateAwaitAsync(string.Empty, async (preparedScripts, configuration) =>
            preparedScripts + Environment.NewLine + new string('\t', TABS_NUMBER) + await getScript(configuration));
        formattedScript += Environment.NewLine;

        return formattedScript;
    }

    /// <summary>
    /// Format custom event data to look pretty
    /// </summary>
    /// <param name="customData">Custom data</param>
    /// <returns>Script code</returns>
    protected string FormatCustomData(ConversionsEventProperties customData)
    {
        List<(string Name, object Value)> getProperties(JObject jObject)
        {
            var result = jObject.ToObject<Dictionary<string, object>>();
            foreach (var pair in result)
            {
                if (pair.Value is JObject nestedObject)
                    result[pair.Key] = getProperties(nestedObject);
                if (pair.Value is JArray nestedArray && nestedArray.OfType<JObject>().Any())
                    result[pair.Key] = nestedArray.OfType<JObject>().Select(obj => getProperties(obj)).ToList();
            }

            return result.Select(pair => (pair.Key, pair.Value)).ToList();
        }

        try
        {
            var customDataObject = JObject.FromObject(customData, new JsonSerializer { NullValueHandling = NullValueHandling.Ignore });

            return FormatEventObject(getProperties(customDataObject));

        }
        catch
        {
            //if something went wrong, just serialize the data without format
            return JsonConvert.SerializeObject(customData, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        }
    }

    /// <summary>
    /// Get configurations
    /// </summary>
    /// <param name="storeId">Store identifier; pass 0 to load all records</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the list of configurations
    /// </returns>
    protected async Task<IList<TikTokPixelConfiguration>> GetConfigurationsAsync(int storeId = 0)
    {
        var key = _staticCacheManager.PrepareKeyForDefaultCache(TikTokPixelDefaults.ConfigurationsCacheKey, storeId);

        var query = _tiktokPixelConfigurationRepository.Table;

        //filter by the store
        if (storeId > 0)
            query = query.Where(configuration => configuration.StoreId == storeId);

        query = query.OrderBy(configuration => configuration.Id);

        return await _staticCacheManager.GetAsync(key, async () => await query.ToListAsync());
    }

    /// <summary>
    /// Format event object to look pretty
    /// </summary>
    /// <param name="properties">Event object properties</param>
    /// <param name="tabsNumber">Tabs number for indentation script</param>
    /// <returns>Script code</returns>
    protected string FormatEventObject(List<(string Name, object Value)> properties, int? tabsNumber = null)
    {
        //local function to format list of objects
        string formatObjectList(List<List<(string Name, object Value)>> objectList)
        {
            var formattedList = objectList.Aggregate(string.Empty, (preparedObjects, propertiesList) =>
            {
                if (propertiesList != null)
                {
                    var value = FormatEventObject(propertiesList, (tabsNumber ?? TABS_NUMBER) + 1);
                    preparedObjects += $"{Environment.NewLine}{new string('\t', (tabsNumber ?? TABS_NUMBER) + 1)}{value},";
                }

                return preparedObjects;
            }).TrimEnd(',');
            return $"[{formattedList}]";
        }

        //format single object
        var formattedObject = properties.Aggregate(string.Empty, (preparedObject, property) =>
        {
            if (!string.IsNullOrEmpty(property.Value?.ToString()))
            {
                //format property value
                var value = property.Value is string valueString
                    ? $"'{valueString.Replace("'", "\\'")}'"
                    : property.Value is List<List<(string Name, object Value)>> valueList
                    ? formatObjectList(valueList)
                    : property.Value is decimal valueDecimal
                    ? valueDecimal.ToString("F", CultureInfo.InvariantCulture)
                    : property.Value.ToString().ToLowerInvariant();

                //format object property
                preparedObject += $"{Environment.NewLine}{new string('\t', (tabsNumber ?? TABS_NUMBER) + 1)}{property.Name}: {value},";
            }

            return preparedObject;
        }).TrimEnd(',');

        return $"{{{formattedObject}{Environment.NewLine}{new string('\t', tabsNumber ?? TABS_NUMBER)}}}";
    }

    /// <summary>
    /// Prepare Pixel script and send requests to Conversions API for the passed event
    /// </summary>
    /// <param name="prepareModel">Function to prepare model</param>
    /// <param name="eventName">Event name</param>
    /// <param name="storeId">Store identifier; pass null to load records for the current store</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the value whether handling was successful
    /// </returns>
    protected async Task<bool> HandleEventAsync(Func<Task<List<ConversionsEventData>>> prepareModel, string eventName, int? storeId = null)
    {
        storeId ??= (await _storeContext.GetCurrentStoreAsync()).Id;
        var configurations = (await GetConfigurationsAsync(storeId ?? 0)).Where(configuration => eventName switch
        {
            TikTokPixelDefaults.ADD_TO_CART => configuration.TrackAddToCart,
            TikTokPixelDefaults.ADD_TO_WISHLIST => configuration.TrackAddToWishlist,
            TikTokPixelDefaults.PURCHASE => configuration.TrackPurchase,
            TikTokPixelDefaults.VIEW_CONTENT => configuration.TrackViewContent,
            TikTokPixelDefaults.INITIATE_CHECKOUT => configuration.TrackInitiateCheckout,
            TikTokPixelDefaults.SEARCH => configuration.TrackSearch,
            TikTokPixelDefaults.CONTACT => configuration.TrackContact,
            TikTokPixelDefaults.COMPLETE_REGISTRATION => configuration.TrackCompleteRegistration,
            _ => false
        }).ToList();

        var conversionsApiConfigurations = configurations.Where(configuration => configuration.ConversionsApiEnabled).ToList();
        var pixelConfigurations = configurations.Where(configuration => configuration.PixelScriptEnabled).ToList();
        if (!conversionsApiConfigurations.Any() && !pixelConfigurations.Any())
            return false;

        var model = await prepareModel();

        if (pixelConfigurations.Any())
            await PreparePixelScriptAsync(model);

        foreach (var config in conversionsApiConfigurations)
            await _tiktokConversionsHttpClient.SendEventAsync(config, new ConversionsEvent { EventSourceId = config.PixelId, Data = model });

        return true;
    }

    #endregion

    #region Methods

    #region Scripts

    /// <summary>
    /// Prepare Tiktok Pixel script
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the script code
    /// </returns>
    public async Task<string> PrepareScriptAsync()
    {
        return await HandleFunctionAsync(async () =>
        {
            //get the enabled configurations
            var store = await _storeContext.GetCurrentStoreAsync();
            var configurations = await (await GetConfigurationsAsync(store.Id)).WhereAwait(async configuration =>
            {
                if (!configuration.PixelScriptEnabled)
                    return false;

                if (!configuration.DisableForUsersNotAcceptingCookieConsent)
                    return true;

                //don't display Pixel for users who did not accept Cookie Consent
                var cookieConsentAccepted = await _genericAttributeService.GetAttributeAsync<bool>(await _workContext.GetCurrentCustomerAsync(),
                        NopCustomerDefaults.EuCookieLawAcceptedAttribute, store.Id);
                return cookieConsentAccepted;
            }).ToListAsync();
            if (!configurations.Any())
                return string.Empty;

            // Pixel script
            // By default, the pixel base code will always include "Page View" events, which track when a visitor lands on your website. Therefore, no additional event code is needed to track "Page View."
            return string.Join(string.Empty, configurations.Select(config => $@"
    <!-- TikTok Pixel Base Code -->
    <script>
        !function (w, d, t) {{
          w.TiktokAnalyticsObject=t;var ttq=w[t]=w[t]||[];ttq.methods=[""page"",""track"",""identify"",""instances"",""debug"",""on"",""off"",""once"",""ready"",""alias"",""group"",""enableCookie"",""disableCookie""],ttq.setAndDefer=function(t,e){{t[e]=function(){{t.push([e].concat(Array.prototype.slice.call(arguments,0)))}}}};for(var i=0;i<ttq.methods.length;i++)ttq.setAndDefer(ttq,ttq.methods[i]);ttq.instance=function(t){{for(var e=ttq._i[t]||[],n=0;n<ttq.methods.length;n++)ttq.setAndDefer(e,ttq.methods[n]);return e}},ttq.load=function(e,n){{var i=""https://analytics.tiktok.com/i18n/pixel/events.js"";ttq._i=ttq._i||{{}},ttq._i[e]=[],ttq._i[e]._u=i,ttq._t=ttq._t||{{}},ttq._t[e]=+new Date,ttq._o=ttq._o||{{}},ttq._o[e]=n||{{}};var o=document.createElement(""script"");o.type=""text/javascript"",o.async=!0,o.src=i+""?sdkid=""+e+""&lib=""+t;var a=document.getElementsByTagName(""script"")[0];a.parentNode.insertBefore(o,a)}};

          ttq.load('{config.PixelId}');
          ttq.page();
        }}(window, document, 'ttq');
    </script>
    <!-- End Tiktok Pixel Base Code -->")) + $@"
    <script>
        {await PreparePixelEventCodeAsync(configurations)}
    </script>";
        });
    }

    /// <summary>
    /// Prepare script to init Tiktok Pixel
    /// </summary>
    /// <param name="configurations">Enabled configurations</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the script code
    /// </returns>
    protected async Task<string> PrepareInitScriptAsync(IList<TikTokPixelConfiguration> configurations)
    {
        //prepare init script
        return await FormatScriptAsync(configurations, async configuration =>
        {
            var customer = await _workContext.GetCurrentCustomerAsync();

            var userInfo = configuration.PassUserProperties
                ? $"{{uid: '{customer.CustomerGuid}'}}"
                : configuration.UseAdvancedMatching
                ? $"{await GetUserObjectAsync()}"
                : null;
            return $"ttq.instance('{configuration.PixelId}').identify({userInfo})";
        });
    }

    /// <summary>
    /// Prepare Tiktok Pixel script
    /// </summary>
    /// <param name="widgetZone">Widget zone to place script</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the script code
    /// </returns>
    public async Task<string> PrepareCustomEventsScriptAsync(string widgetZone)
    {
        return await HandleFunctionAsync(async () =>
        {
            var customEvents = await (await GetConfigurationsAsync()).SelectManyAwait(async configuration => await GetCustomEventsAsync(configuration.Id, widgetZone)).ToListAsync();
            foreach (var customEvent in customEvents)
                await PrepareTrackedEventScriptAsync(customEvent.EventName, string.Empty, isCustomEvent: true);

            return string.Empty;
        });
    }

    #endregion

    #region Conversions API

    /// <summary>
    /// Send add to cart events
    /// </summary>
    /// <param name="shoppingCartItem">Shopping cart item</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task SendAddToCartEventAsync(ShoppingCartItem shoppingCartItem)
    {
        await HandleFunctionAsync(async () =>
        {
            var eventName = shoppingCartItem.ShoppingCartTypeId == (int)ShoppingCartType.ShoppingCart
                ? TikTokPixelDefaults.ADD_TO_CART
                : TikTokPixelDefaults.ADD_TO_WISHLIST;

            return await HandleEventAsync(() => PrepareAddToCartEventModelAsync(shoppingCartItem), eventName, shoppingCartItem.StoreId);
        });
    }

    /// <summary>
    /// Send purchase events
    /// </summary>
    /// <param name="order">Order</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task SendPurchaseEventAsync(Order order)
    {
        await HandleFunctionAsync(() =>
             HandleEventAsync(() => PreparePurchaseModelAsync(order), TikTokPixelDefaults.PURCHASE, order.StoreId));
    }

    /// <summary>
    /// Send view content events
    /// </summary>
    /// <param name="productDetailsModel">Product details model</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task SendViewContentEventAsync(ProductDetailsModel productDetailsModel)
    {
        await HandleFunctionAsync(() =>
            HandleEventAsync(() => PrepareViewContentModelAsync(productDetailsModel), TikTokPixelDefaults.VIEW_CONTENT));
    }

    /// <summary>
    /// Send initiate checkout events
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task SendInitiateCheckoutEventAsync()
    {
        await HandleFunctionAsync(() =>
            HandleEventAsync(() => PrepareInitiateCheckoutModelAsync(), TikTokPixelDefaults.INITIATE_CHECKOUT));
    }

    /// <summary>
    /// Send search events
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task SendSearchEventAsync(string searchTerm)
    {
        await HandleFunctionAsync(() =>
            HandleEventAsync(() => PrepareSearchModelAsync(searchTerm), TikTokPixelDefaults.SEARCH));
    }

    /// <summary>
    /// Send contact events
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task SendContactEventAsync()
    {
        await HandleFunctionAsync(() =>
            HandleEventAsync(() => PrepareContactModelAsync(), TikTokPixelDefaults.CONTACT));
    }

    /// <summary>
    /// Send complete registration events
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task SendCompleteRegistrationEventAsync()
    {
        await HandleFunctionAsync(() =>
            HandleEventAsync(() => PrepareCompleteRegistrationModelAsync(), TikTokPixelDefaults.COMPLETE_REGISTRATION));
    }

    #endregion

    #region Configuration

    /// <summary>
    /// Get configurations
    /// </summary>
    /// <param name="storeId">Store identifier; pass 0 to load all records</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the paged list of configurations
    /// </returns>
    public async Task<IPagedList<TikTokPixelConfiguration>> GetPagedConfigurationsAsync(int storeId = 0, int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = _tiktokPixelConfigurationRepository.Table;

        //filter by the store
        if (storeId > 0)
            query = query.Where(configuration => configuration.StoreId == storeId);

        query = query.OrderBy(configuration => configuration.Id);

        return await query.ToPagedListAsync(pageIndex, pageSize);
    }

    /// <summary>
    /// Get a configuration by the identifier
    /// </summary>
    /// <param name="configurationId">Configuration identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the configuration
    /// </returns>
    public async Task<TikTokPixelConfiguration> GetConfigurationByIdAsync(int configurationId)
    {
        if (configurationId == 0)
            return null;

        return await _staticCacheManager.GetAsync(_staticCacheManager.PrepareKeyForDefaultCache(TikTokPixelDefaults.ConfigurationCacheKey, configurationId), async () =>
            await _tiktokPixelConfigurationRepository.GetByIdAsync(configurationId));
    }

    /// <summary>
    /// Insert the configuration
    /// </summary>
    /// <param name="configuration">Configuration</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task InsertConfigurationAsync(TikTokPixelConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        await _tiktokPixelConfigurationRepository.InsertAsync(configuration, false);
        await _staticCacheManager.RemoveByPrefixAsync(TikTokPixelDefaults.PrefixCacheKey);
    }

    /// <summary>
    /// Update the configuration
    /// </summary>
    /// <param name="configuration">Configuration</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task UpdateConfigurationAsync(TikTokPixelConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        await _tiktokPixelConfigurationRepository.UpdateAsync(configuration, false);
        await _staticCacheManager.RemoveByPrefixAsync(TikTokPixelDefaults.PrefixCacheKey);
    }

    /// <summary>
    /// Delete the configuration
    /// </summary>
    /// <param name="configuration">Configuration</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task DeleteConfigurationAsync(TikTokPixelConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        await _tiktokPixelConfigurationRepository.DeleteAsync(configuration, false);
        await _staticCacheManager.RemoveByPrefixAsync(TikTokPixelDefaults.PrefixCacheKey);
    }

    /// <summary>
    /// Get configuration custom events
    /// </summary>
    /// <param name="configurationId">Configuration identifier</param>
    /// <param name="widgetZone">Widget zone name; pass null to load all records</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the list of custom events
    /// </returns>
    public async Task<IList<CustomEvent>> GetCustomEventsAsync(int configurationId, string widgetZone = null)
    {
        var cachedCustomEvents = await _staticCacheManager.GetAsync(_staticCacheManager.PrepareKeyForDefaultCache(TikTokPixelDefaults.CustomEventsCacheKey, configurationId), async () =>
        {
            //load configuration custom events
            var configuration = await GetConfigurationByIdAsync(configurationId);
            var customEventsValue = configuration?.CustomEvents ?? string.Empty;
            var customEvents = JsonConvert.DeserializeObject<List<CustomEvent>>(customEventsValue) ?? new List<CustomEvent>();
            return customEvents;
        });

        //filter by the widget zone
        if (!string.IsNullOrEmpty(widgetZone))
            cachedCustomEvents = cachedCustomEvents.Where(customEvent => customEvent.WidgetZones?.Contains(widgetZone) ?? false).ToList();

        return cachedCustomEvents;
    }

    /// <summary>
    /// Save configuration custom events
    /// </summary>
    /// <param name="configurationId">Configuration identifier</param>
    /// <param name="eventName">Event name</param>
    /// <param name="widgetZones">Widget zones names</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task SaveCustomEventsAsync(int configurationId, string eventName, IList<string> widgetZones)
    {
        if (string.IsNullOrEmpty(eventName))
            return;

        var configuration = await GetConfigurationByIdAsync(configurationId);
        if (configuration == null)
            return;

        //load configuration custom events
        var customEventsValue = configuration.CustomEvents ?? string.Empty;
        var customEvents = JsonConvert.DeserializeObject<List<CustomEvent>>(customEventsValue) ?? new List<CustomEvent>();

        //try to get an event by the passed name
        var customEvent = customEvents
            .FirstOrDefault(customEvent => eventName.Equals(customEvent.EventName, StringComparison.InvariantCultureIgnoreCase));
        if (customEvent == null)
        {
            //create new one if not exist
            customEvent = new CustomEvent { EventName = eventName };
            customEvents.Add(customEvent);
        }

        //update widget zones of this event
        customEvent.WidgetZones = widgetZones ?? new List<string>();

        //or delete an event
        if (!customEvent.WidgetZones.Any())
            customEvents.Remove(customEvent);

        //update configuration 
        configuration.CustomEvents = JsonConvert.SerializeObject(customEvents);
        await UpdateConfigurationAsync(configuration);
        await _staticCacheManager.RemoveByPrefixAsync(TikTokPixelDefaults.PrefixCacheKey);
        await _staticCacheManager.RemoveByPrefixAsync(WidgetModelDefaults.WidgetPrefixCacheKey);
    }

    /// <summary>
    /// Get used widget zones for all custom events
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the list of widget zones names
    /// </returns>
    public async Task<IList<string>> GetCustomEventsWidgetZonesAsync()
    {
        return await _staticCacheManager.GetAsync(_staticCacheManager.PrepareKeyForDefaultCache(TikTokPixelDefaults.WidgetZonesCacheKey), async () =>
        {
            //load custom events and their widget zones
            var configurations = await GetConfigurationsAsync();
            var customEvents = await configurations.SelectManyAwait(async configuration => await GetCustomEventsAsync(configuration.Id)).ToListAsync();
            var widgetZones = await customEvents.SelectMany(customEvent => customEvent.WidgetZones).Distinct().ToListAsync();

            return widgetZones;
        });
    }

    #endregion

    #endregion

}
