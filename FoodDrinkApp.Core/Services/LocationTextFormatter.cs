namespace FoodDrinkApp.Services;

/// <summary>
/// Formats location coordinates and deterministic fallback address text for display.
/// </summary>
public static class LocationTextFormatter
{
    /// <summary>
    /// Formats coordinates with stable precision for visual and screen-reader output.
    /// </summary>
    public static string FormatCoordinates(double latitude, double longitude) =>
        $"Latitude {latitude:F5}, longitude {longitude:F5}";

    /// <summary>
    /// Provides deterministic fallback text for emulator coordinates when geocoding is unavailable.
    /// </summary>
    public static string BuildFallbackAddress(double latitude, double longitude)
    {
        if (IsNear(latitude, longitude, 37.422, -122.084, 0.08))
        {
            return "United States / California / Mountain View";
        }

        if (latitude is >= 37.0 and <= 38.2 && longitude is >= -123.2 and <= -121.5)
        {
            return "United States / California / San Francisco Bay Area";
        }

        if (latitude is >= 18 and <= 54 && longitude is >= 73 and <= 135)
        {
            return "China / Current city requires a real device or available geocoding service";
        }

        return "Coordinates were found, but country and city were not returned by this device.";
    }

    private static bool IsNear(
        double latitude,
        double longitude,
        double targetLatitude,
        double targetLongitude,
        double tolerance)
    {
        return Math.Abs(latitude - targetLatitude) <= tolerance &&
               Math.Abs(longitude - targetLongitude) <= tolerance;
    }
}
