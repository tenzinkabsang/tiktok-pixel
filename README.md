# TikTok Pixel / Conversions API Plugin for nopCommerce

The **TikTok Pixel / Conversions API** plugin for [nopCommerce](https://www.nopcommerce.com/) lets you share web events from a web browser via the TikTok Pixel, while the Conversions API lets you share web events directly from your server. Both methods are fully supported by this plugin.

TikTok Pixel allows you to receive information about the actions taken on your store to make your TikTok Ads more relevant to your audience. It helps you understand the behavior of people who visit your store and which advertising strategy works best to reach your business goals.

Tracked conversions appear in the **TikTok Ads Manager** and in the **TikTok Analytics** dashboard, where they can be used to:

- Measure the effectiveness of your ads
- Define custom audiences for ad targeting
- Run dynamic ad campaigns
- Analyze the effectiveness of your website's conversion funnels

**[View on nopCommerce Marketplace](https://www.nopcommerce.com/en/tiktok-pixel)**

---

## Compatibility

| Plugin Version | nopCommerce Version | .NET    |
| -------------- | ------------------- | ------- |
| 4.80.x         | 4.80                | .NET 8  |

## Features

### Tracking Methods

- **TikTok Pixel (browser-side)** — injects the TikTok base pixel script and fires events from the visitor's browser.
- **Conversions API (server-side)** — sends events directly from your server to TikTok for more reliable tracking, even when browser-side tracking is blocked.
- Both methods can be used simultaneously for maximum data coverage.

### Tracked Events

| Event                 | Description                                              |
| --------------------- | -------------------------------------------------------- |
| **AddToCart**          | A product is added to the shopping cart                  |
| **PlaceAnOrder**       | A purchase is made / checkout flow is completed          |
| **ViewContent**        | A visitor views a product page                           |
| **AddToWishList**      | A product is added to the wishlist                       |
| **InitiateCheckout**   | A visitor enters the checkout flow                       |
| **Search**             | A search is performed                                    |
| **Contact**            | A visitor submits the contact form                       |
| **CompleteRegistration**| A visitor completes the registration form               |
| **Custom Events**      | Define your own custom events with flexible configuration|

### Additional Capabilities

- **Multi-store support** — configure separate Pixel IDs per store.
- **Advanced Matching** — pass hashed customer data (email, phone, etc.) to improve attribution.
- **User Data Forwarding** — optionally send customer properties alongside events.
- **GDPR / Cookie Consent** — option to disable the pixel for users who have not accepted cookie consent.
- **Custom Events** — create and manage custom event definitions beyond the standard tracked events.

## Installation

1. Download the plugin archive.
2. Go to **Admin Area → Configuration → Local Plugins**.
3. Upload the plugin archive using the **"Upload plugin or theme"** button.
4. Scroll down through the list of plugins to find the newly installed plugin.
5. Click the **"Install"** button to install the plugin.

## Configuration

1. Navigate to **Admin Area → Configuration → Widgets** and click **"Configure"** next to *TikTok Pixel*.
2. Click **"Add new configuration"** and fill in the required fields:
   - **Pixel ID** — your TikTok Pixel identifier (found in TikTok Ads Manager → Events Manager).
   - **Access Token** — required if you want to use the Conversions API (generated in TikTok Ads Manager).
   - **Store** — select which store this configuration applies to.
3. Enable or disable individual events (AddToCart, Purchase, ViewContent, etc.) based on your tracking needs.
4. Optionally enable **Advanced Matching** and **Pass User Properties** for improved attribution.
5. Optionally enable the **GDPR cookie consent** setting if applicable.
6. Save the configuration.

## How It Works

- **Browser-side (Pixel):** The plugin injects the TikTok Pixel base code into your storefront pages. When a tracked event occurs (e.g., a product is added to the cart), the corresponding pixel event is fired in the visitor's browser.
- **Server-side (Conversions API):** When enabled, the plugin simultaneously sends the same event data to TikTok's [Events API](https://business-api.tiktok.com/open_api/v1.3/event/track/) directly from your server, ensuring events are captured even if the browser blocks tracking scripts.

## Requirements

- nopCommerce **4.80**
- .NET **8.0**

## Author

**Tenzin Kabsang**

## License

This project is licensed under the **GNU General Public License v3.0** — see the [LICENSE](LICENSE) file for details.
