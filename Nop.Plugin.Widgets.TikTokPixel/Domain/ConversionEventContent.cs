using Newtonsoft.Json;

namespace Nop.Plugin.Widgets.TikTokPixel.Domain;

public record ConversionEventContent
{
    /// <summary>
    /// Gets or sets the content ids associated with the event, such as product skus for items in an addtocart event. if content_type is a product, then your content ids must be an array with a single string value. otherwise, this array can contain any number of string values
    /// </summary>
    [JsonProperty(PropertyName = "content_id")]
    public string ContentId { get; set; }

    /// <summary>
    /// Gets or sets the content type
    /// </summary>
    [JsonProperty(PropertyName = "content_type")]
    public string ContentType { get; set; }

    /// <summary>
    /// Gets or sets the name of the page or product associated with the event
    /// </summary>
    [JsonProperty(PropertyName = "content_name")]
    public string ContentName { get; set; }

    [JsonProperty(PropertyName = "price")]
    public decimal? Price { get; set; }

    [JsonProperty(PropertyName = "quantity")]
    public int? Quantity { get; set; }

    /// <summary>
    /// Gets or sets the category of the content associated with the event
    /// </summary>
    [JsonProperty(PropertyName = "content_category")]
    public string ContentCategory { get; set; }

    [JsonProperty(PropertyName = "description")]
    public string Description { get; set; }

    /// <summary>
    /// String enum: 'in stock', 'available for order', 'preorder', 'out of stock', 'discontinued'
    /// </summary>
    [JsonProperty(PropertyName = "availability")]
    public string Availability { get; set; }

    /// <summary>
    /// String enum: 'new', 'refurbished', 'used'
    /// </summary>
    [JsonProperty(PropertyName = "condition")]
    public string Condition { get; set; }

    [JsonProperty(PropertyName = "brand")]
    public string Brand { get; set; }

    [JsonProperty(PropertyName = "image_link")]
    public string ImageLink { get; set; }

    [JsonProperty(PropertyName = "product_type")]
    public string ProductType { get; set; }

    [JsonProperty(PropertyName = "link")]
    public string ProductLink { get; set; }
}
