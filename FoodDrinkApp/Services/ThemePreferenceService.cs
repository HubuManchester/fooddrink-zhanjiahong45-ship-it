namespace FoodDrinkApp.Services;

/// <summary>
/// Persists and applies the app theme preference, including the default system-following mode.
/// </summary>
public static class ThemePreferenceService
{
    private const string PreferenceKey = "app_theme";
    private const string SystemChoice = "System";
    private const string LightChoice = "Light";
    private const string DarkChoice = "Dark";

    /// <summary>
    /// Applies the saved theme choice, defaulting to the operating system theme.
    /// </summary>
    public static void ApplySavedTheme()
    {
        ApplyTheme(Preferences.Get(PreferenceKey, SystemChoice));
    }

    /// <summary>
    /// Saves and applies a theme choice selected from the Settings picker.
    /// </summary>
    public static void SaveAndApplyFromIndex(int selectedIndex)
    {
        var choice = selectedIndex switch
        {
            1 => LightChoice,
            2 => DarkChoice,
            _ => SystemChoice
        };

        Preferences.Set(PreferenceKey, choice);
        ApplyTheme(choice);
    }

    /// <summary>
    /// Gets the picker index for the saved choice.
    /// </summary>
    public static int SavedIndex => Preferences.Get(PreferenceKey, SystemChoice) switch
    {
        LightChoice => 1,
        DarkChoice => 2,
        _ => 0
    };

    private static void ApplyTheme(string choice)
    {
        if (Application.Current is null)
        {
            return;
        }

        Application.Current.UserAppTheme = choice switch
        {
            LightChoice => AppTheme.Light,
            DarkChoice => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
    }
}
