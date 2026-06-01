using FoodDrinkApp.Services;

namespace FoodDrinkApp.Tests;

public sealed class LocationTextFormatterTests
{
    [Fact]
    public void FormatCoordinates_rounds_to_five_decimal_places()
    {
        var text = LocationTextFormatter.FormatCoordinates(37.4219999, -122.0840575);

        Assert.Equal("Latitude 37.42200, longitude -122.08406", text);
    }

    [Fact]
    public void BuildFallbackAddress_identifies_mountain_view_emulator()
    {
        var address = LocationTextFormatter.BuildFallbackAddress(37.422, -122.084);

        Assert.Equal("United States / California / Mountain View", address);
    }

    [Fact]
    public void BuildFallbackAddress_identifies_bay_area_coordinates()
    {
        var address = LocationTextFormatter.BuildFallbackAddress(37.7749, -122.4194);

        Assert.Equal("United States / California / San Francisco Bay Area", address);
    }

    [Fact]
    public void BuildFallbackAddress_identifies_china_geocoding_fallback()
    {
        var address = LocationTextFormatter.BuildFallbackAddress(30.5928, 114.3055);

        Assert.Equal("China / Current city requires a real device or available geocoding service", address);
    }

    [Fact]
    public void BuildFallbackAddress_handles_unknown_coordinates()
    {
        var address = LocationTextFormatter.BuildFallbackAddress(51.5072, -0.1276);

        Assert.Equal("Coordinates were found, but country and city were not returned by this device.", address);
    }
}
