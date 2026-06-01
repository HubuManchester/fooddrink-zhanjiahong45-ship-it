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
        LocationTextFormatter.FormatCoordinates(location.Latitude, location.Longitude);

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
    public static string BuildFallbackAddress(Location location) =>
        LocationTextFormatter.BuildFallbackAddress(location.Latitude, location.Longitude);
}
