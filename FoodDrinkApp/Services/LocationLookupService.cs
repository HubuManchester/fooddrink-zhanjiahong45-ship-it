using Microsoft.Maui.Devices.Sensors;

namespace FoodDrinkApp.Services;

/// <summary>
/// Represents the display-ready location details captured for a meal.
/// </summary>
/// <param name="Latitude">The captured latitude.</param>
/// <param name="Longitude">The captured longitude.</param>
/// <param name="CoordinatesText">The formatted coordinate text shown in the UI.</param>
/// <param name="AddressText">The reverse-geocoded or fallback address text shown in the UI.</param>
public sealed record MealLocationResult(
    double Latitude,
    double Longitude,
    string CoordinatesText,
    string AddressText);

/// <summary>
/// Loads the device location and converts it into meal-friendly display text.
/// </summary>
public sealed class LocationLookupService
{
    /// <summary>
    /// Gets the current device location and returns coordinates plus country/city text.
    /// </summary>
    public async Task<MealLocationResult?> GetCurrentMealLocationAsync()
    {
        var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
        var location = await Geolocation.Default.GetLocationAsync(request);

        if (location is null)
        {
            return null;
        }

        return new MealLocationResult(
            location.Latitude,
            location.Longitude,
            FormatCoordinates(location),
            await BuildAddressTextAsync(location));
    }

    /// <summary>
    /// Formats coordinates with stable precision for visual and screen-reader output.
    /// </summary>
    public static string FormatCoordinates(Location location) =>
        $"Latitude {location.Latitude:F5}, longitude {location.Longitude:F5}";

    /// <summary>
    /// Builds display address text from reverse geocoding, falling back to known demo regions.
    /// </summary>
    public static async Task<string> BuildAddressTextAsync(Location location)
    {
        try
        {
            var placemarks = await Geocoding.Default.GetPlacemarksAsync(location);
            var placemark = placemarks?.FirstOrDefault();
            var address = FormatPlacemark(placemark);

            if (!string.IsNullOrWhiteSpace(address))
            {
                return address;
            }
        }
        catch (Exception ex)
        {
            AppLog.Error("Reverse geocode location", ex);
        }

        return BuildFallbackAddress(location);
    }

    /// <summary>
    /// Formats a MAUI placemark into a compact country, region, city, and street string.
    /// </summary>
    public static string FormatPlacemark(Placemark? placemark)
    {
        if (placemark is null)
        {
            return string.Empty;
        }

        var parts = new[]
        {
            placemark.CountryName,
            placemark.AdminArea,
            placemark.Locality,
            placemark.SubLocality,
            placemark.Thoroughfare
        }
        .Where(part => !string.IsNullOrWhiteSpace(part))
        .Distinct()
        .ToArray();

        return parts.Length == 0 ? string.Empty : string.Join(" / ", parts);
    }

    /// <summary>
    /// Provides deterministic fallback text for emulator coordinates when geocoding is unavailable.
    /// </summary>
    public static string BuildFallbackAddress(Location location)
    {
        if (IsNear(location, 37.422, -122.084, 0.08))
        {
            return "United States / California / Mountain View";
        }

        if (location.Latitude is >= 37.0 and <= 38.2 && location.Longitude is >= -123.2 and <= -121.5)
        {
            return "United States / California / San Francisco Bay Area";
        }

        if (location.Latitude is >= 18 and <= 54 && location.Longitude is >= 73 and <= 135)
        {
            return "China / Current city requires a real device or available geocoding service";
        }

        return "Coordinates were found, but country and city were not returned by this device.";
    }

    private static bool IsNear(Location location, double latitude, double longitude, double tolerance)
    {
        return Math.Abs(location.Latitude - latitude) <= tolerance &&
               Math.Abs(location.Longitude - longitude) <= tolerance;
    }
}
