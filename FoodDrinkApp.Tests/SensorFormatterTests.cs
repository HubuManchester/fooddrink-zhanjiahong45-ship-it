using System.Numerics;
using FoodDrinkApp.Services;

namespace FoodDrinkApp.Tests;

public sealed class SensorFormatterTests
{
    [Fact]
    public void Vector3_formats_to_two_decimal_places()
    {
        var text = SensorFormatter.Vector3("Acceleration", new Vector3(1.234f, -2.345f, 0));

        Assert.Equal("Acceleration: X 1.23, Y -2.35, Z 0.00", text);
    }

    [Theory]
    [InlineData(0, "N")]
    [InlineData(90, "E")]
    [InlineData(180, "S")]
    [InlineData(270, "W")]
    [InlineData(360, "N")]
    public void Cardinal_returns_expected_direction(double degrees, string expected)
    {
        Assert.Equal(expected, SensorFormatter.Cardinal(degrees));
    }
}
