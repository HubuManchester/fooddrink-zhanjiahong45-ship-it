namespace FoodDrinkApp.Services;

/// <summary>
/// Calculates WCAG contrast ratios for foreground/background colour pairs.
/// </summary>
public static class ContrastRatio
{
    /// <summary>
    /// Returns the WCAG contrast ratio between two six-digit hex colours.
    /// </summary>
    public static double Between(string hexA, string hexB) =>
        Ratio(RelativeLuminance(hexA), RelativeLuminance(hexB));

    private static double Ratio(double luminanceA, double luminanceB)
    {
        var lighter = Math.Max(luminanceA, luminanceB);
        var darker = Math.Min(luminanceA, luminanceB);
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double RelativeLuminance(string hex)
    {
        var (red, green, blue) = Rgb(hex);
        return 0.2126 * Linear(red) + 0.7152 * Linear(green) + 0.0722 * Linear(blue);
    }

    private static double Linear(int channel)
    {
        var value = channel / 255.0;
        return value <= 0.03928
            ? value / 12.92
            : Math.Pow((value + 0.055) / 1.055, 2.4);
    }

    private static (int Red, int Green, int Blue) Rgb(string hex)
    {
        var normalized = hex.Trim().TrimStart('#');

        if (normalized.Length != 6)
        {
            throw new ArgumentException("Use a six-digit hex colour such as #3A2518.", nameof(hex));
        }

        return (
            Convert.ToInt32(normalized[..2], 16),
            Convert.ToInt32(normalized[2..4], 16),
            Convert.ToInt32(normalized[4..6], 16));
    }
}
