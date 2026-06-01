using System.Numerics;

namespace FoodDrinkApp.Services;

/// <summary>
/// Formats sensor readings into stable, screen-reader-friendly text.
/// </summary>
public static class SensorFormatter
{
    /// <summary>
    /// Formats a three-axis sensor vector with stable two-decimal precision.
    /// </summary>
    public static string Vector3(string label, Vector3 value) =>
        $"{label}: X {value.X:F2}, Y {value.Y:F2}, Z {value.Z:F2}";

    /// <summary>
    /// Formats a compass heading with its nearest cardinal direction.
    /// </summary>
    public static string Heading(double degrees) =>
        $"Compass heading: {degrees:F0} deg ({Cardinal(degrees)})";

    /// <summary>
    /// Converts degrees into the nearest eight-point compass direction.
    /// </summary>
    public static string Cardinal(double degrees)
    {
        string[] directions = ["N", "NE", "E", "SE", "S", "SW", "W", "NW"];
        var normalized = ((degrees % 360) + 360) % 360;
        return directions[(int)Math.Round(normalized / 45) % directions.Length];
    }
}
