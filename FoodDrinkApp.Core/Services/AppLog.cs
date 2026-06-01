namespace FoodDrinkApp.Services;

/// <summary>
/// Writes internal diagnostic messages without changing the user-facing error flow.
/// </summary>
public static class AppLog
{
    /// <summary>
    /// Records an exception with enough context to debug a graceful fallback.
    /// </summary>
    public static void Error(string context, Exception ex) =>
        System.Diagnostics.Debug.WriteLine($"[NutriBite][ERROR] {context}: {ex}");
}
