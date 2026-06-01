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
            const string helpText = "Search foods, swipe cards to favorite them, capture a food photo for on-device recognition, use sensors on the hardware tab, and set text size up to two hundred percent in settings.";
            await SpeechService.SpeakAsync(helpText);
            SetStatus("Reading help instructions aloud.");
        }
        catch
        {
            SetStatus("Help instructions could not be read aloud right now.");
        }
    }

    private void OnTestErrorClicked(object? sender, EventArgs e)
    {
        try
        {
            throw new FileNotFoundException("Demo file missing for internal error handling.");
        }
        catch
        {
            SetStatus("Demo error handled gracefully. The app keeps running.");
        }
    }

    private void SetStatus(string message)
    {
        HelpStatusLabel.Text = message;
        SemanticScreenReader.Announce(message);
    }
}
