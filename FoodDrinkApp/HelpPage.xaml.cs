using FoodDrinkApp.Services;

namespace FoodDrinkApp;

public partial class HelpPage : ContentPage
{
    public HelpPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        AccessibilityService.ApplyFontScale(this);
    }

    private async void OnReadHelpClicked(object? sender, EventArgs e)
    {
        try
        {
            const string helpText = "Search and filter foods, swipe cards to manage favorites, add or edit records with realistic nutrition values, open details for macro summaries, capture a food photo for on-device recognition, load location, use sensors on the hardware tab, enable shake suggestions, and set theme plus text size up to two hundred percent in settings.";
            await SpeechService.SpeakAsync(helpText);
            SetStatus("Reading help instructions aloud.");
        }
        catch (Exception ex)
        {
            AppLog.Error("Read help page aloud", ex);
            SetStatus("Help instructions could not be read aloud right now.");
        }
    }

    private void OnTestErrorClicked(object? sender, EventArgs e)
    {
        try
        {
            throw new FileNotFoundException("Demo file missing for internal error handling.");
        }
        catch (Exception ex)
        {
            AppLog.Error("Run handled error demo", ex);
            SetStatus("Demo error handled gracefully. The app keeps running.");
        }
    }

    private void SetStatus(string message)
    {
        HelpStatusLabel.Text = message;
        SemanticScreenReader.Announce(message);
    }
}
