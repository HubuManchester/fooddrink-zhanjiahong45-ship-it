namespace FoodDrinkApp.Services;

/// <summary>
/// Stores the remote catalogue endpoint used by the catalogue service.
/// </summary>
public static class MockApiConfig
{
    // This can be replaced with a mockapi.io Resource endpoint later.
    // Example: https://682xxxx.mockapi.io/api/v1/foods
    /// <summary>
    /// Gets the configured HTTPS food catalogue endpoint, or an empty string for local fallback mode.
    /// </summary>
    public const string EndpointUrl = "https://raw.githubusercontent.com/HubuManchester/fooddrink-zhanjiahong45-ship-it/main/data/foods.json";

    /// <summary>
    /// Gets additional read-only mirrors for the same public catalogue.
    /// </summary>
    public static readonly IReadOnlyList<string> ReadOnlyMirrorUrls =
    [
        "https://cdn.jsdelivr.net/gh/HubuManchester/fooddrink-zhanjiahong45-ship-it@main/data/foods.json",
        "https://gh-proxy.com/https://raw.githubusercontent.com/HubuManchester/fooddrink-zhanjiahong45-ship-it/main/data/foods.json",
        "https://ghproxy.net/https://raw.githubusercontent.com/HubuManchester/fooddrink-zhanjiahong45-ship-it/main/data/foods.json"
    ];

    /// <summary>
    /// Gets whether a remote endpoint is available for catalogue operations.
    /// </summary>
    public static bool IsConfigured => !string.IsNullOrWhiteSpace(EndpointUrl);

    /// <summary>
    /// Gets whether the configured endpoint supports REST-style item lookup and writes.
    /// </summary>
    public static bool SupportsItemEndpoint => IsConfigured && !IsStaticJsonEndpoint;

    private static bool IsStaticJsonEndpoint =>
        Uri.TryCreate(EndpointUrl, UriKind.Absolute, out var uri) &&
        uri.AbsolutePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
}
