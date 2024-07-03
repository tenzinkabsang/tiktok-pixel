using System.Text;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Plugin.Widgets.TikTokPixel.Domain;

namespace Nop.Plugin.Widgets.TikTokPixel.Services;

/// <summary>
/// Represents the HTTP client to request TikTok Conversions API
/// </summary>
public class TikTokConversionsHttpClient
{
    #region Fields

    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerSettings _tiktokSerializerSetting = new() { NullValueHandling = NullValueHandling.Ignore };

    #endregion

    #region Ctor

    public TikTokConversionsHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(TikTokPixelDefaults.ConversionsApiBaseAddress);
    }

    #endregion

    #region Method

    /// <summary>
    /// Send event through conversions api
    /// </summary>
    /// <param name="tikTokPixelConfiguration">TikTok pixel configuration object</param>
    /// <param name="conversionsEvent">Conversions api event object</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the asynchronous task whose result contains the response details
    /// </returns>
    public async Task<string> SendEventAsync(TikTokPixelConfiguration tiktokPixelConfiguration, ConversionsEvent conversionsEvent)
    {
        var jsonString = JsonConvert.SerializeObject(conversionsEvent, _tiktokSerializerSetting);
        var requestContent = new StringContent(jsonString, Encoding.UTF8, MimeTypes.ApplicationJson);
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, TikTokPixelDefaults.ConversionsApiEventEndpoint) { Content = requestContent };
        httpRequestMessage.Headers.Add("Access-Token", tiktokPixelConfiguration.AccessToken);
        var result = await _httpClient.SendAsync(httpRequestMessage);
        var response = result.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    #endregion
}
