using FoodDrinkApp.Services;

namespace FoodDrinkApp;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
        ThemePicker.SelectedIndex = ThemePreferenceService.SavedIndex;
        TextScalePicker.SelectedIndex = AccessibilityService.TextScaleLevel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        TextScalePicker.SelectedIndex = AccessibilityService.TextScaleLevel;
        ApplyLargeTextState();
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        ThemePreferenceService.SaveAndApplyFromIndex(ThemePicker.SelectedIndex);

        Announce("Theme preference updated.");
    }

    private void OnTextScaleChanged(object? sender, EventArgs e)
    {
        if (TextScalePicker.SelectedIndex < 0)
        {
            return;
        }

        AccessibilityService.TextScaleLevel = TextScalePicker.SelectedIndex;
        ApplyLargeTextState();
        Announce(AccessibilityService.TextScaleLevel == 0
            ? "Standard text size selected."
            : $"Text size set to {AccessibilityService.CurrentTextScale:P0}.");
    }

    private void ApplyLargeTextState()
    {
        AccessibilityService.ApplyFontScale(this);

        LargeTextPreviewTitle.Text = AccessibilityService.TextScaleLevel == 0
            ? "Text size preview"
            : $"Text size preview: {AccessibilityService.CurrentTextScale:P0}";
        LargeTextPreviewBody.Text = AccessibilityService.TextScaleLevel == 0
            ? "Choose a larger text size to enlarge this preview and other page text."
            : "The food, hardware, help, and settings pages will use the same text scale.";
    }

    private void Announce(string message)
    {
        SettingsStatusLabel.Text = message;
        SemanticScreenReader.Announce(message);
    }
}
