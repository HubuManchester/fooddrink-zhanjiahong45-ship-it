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
    [InlineData(-90, "W")]
    [InlineData(0, "N")]
    [InlineData(45, "NE")]
    [InlineData(90, "E")]
    [InlineData(135, "SE")]
    [InlineData(180, "S")]
    [InlineData(225, "SW")]
    [InlineData(270, "W")]
    [InlineData(315, "NW")]
    [InlineData(360, "N")]
    public void Cardinal_returns_expected_direction(double degrees, string expected)
    {
        Assert.Equal(expected, SensorFormatter.Cardinal(degrees));
    }

    [Theory]
    [InlineData(90, "Compass heading: 90 deg (E)")]
    [InlineData(225.4, "Compass heading: 225 deg (SW)")]
    public void Heading_formats_rounded_degrees_and_cardinal_direction(double degrees, string expected)
    {
        Assert.Equal(expected, SensorFormatter.Heading(degrees));
    }
}
