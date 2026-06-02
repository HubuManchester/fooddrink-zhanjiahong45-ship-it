using FoodDrinkApp.Services;

namespace FoodDrinkApp;

public partial class HelpPage : ContentPage
{
    private int speechRequestVersion;
    private bool isReadingHelp;

    public HelpPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        AccessibilityService.ApplyFontScale(this);
    }

    protected override void OnDisappearing()
    {
        speechRequestVersion++;
        SpeechService.Stop();
        SetSpeechButtonState(false);
        base.OnDisappearing();
    }

    private async void OnReadHelpClicked(object? sender, EventArgs e)
    {
        var requestVersion = ++speechRequestVersion;
        var wasReading = isReadingHelp;

        try
        {
            var helpText = BuildHelpNarrationText();
            if (string.IsNullOrWhiteSpace(helpText))
            {
                SetStatus("There is no help text to read right now.");
                return;
            }

            SetSpeechButtonState(true);
            SetStatus(wasReading ? "Restarting help instructions aloud." : "Reading help instructions aloud.");
            await SpeechService.SpeakAsync(helpText);

            if (requestVersion == speechRequestVersion && isReadingHelp)
            {
                SetStatus("Finished reading help instructions.");
            }
        }
        catch (Exception ex)
        {
            AppLog.Error("Read help page aloud", ex);
            SetStatus("Help instructions could not be read aloud right now.");
        }
        finally
        {
            if (requestVersion == speechRequestVersion)
            {
                SetSpeechButtonState(false);
            }
        }
    }

    private void OnStopHelpClicked(object? sender, EventArgs e)
    {
        speechRequestVersion++;
        SpeechService.Stop();
        SetSpeechButtonState(false);
        SetStatus("Reading stopped.");
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

    private void SetSpeechButtonState(bool isReading)
    {
        isReadingHelp = isReading;
        ReadHelpButton.Text = isReading ? "Restart help" : "Read help";
        StopHelpButton.IsEnabled = isReading;
        SemanticProperties.SetDescription(ReadHelpButton, isReading ? "Restart help" : "Read help");
        SemanticProperties.SetHint(ReadHelpButton, isReading
            ? "Restart the spoken help instructions from the beginning"
            : "Read the main help instructions aloud");
    }

    private string BuildHelpNarrationText()
    {
        var textParts = new List<string>();
        CollectLabelText(HelpContentLayout, textParts);
        return string.Join(" ", textParts);
    }

    private void CollectLabelText(Element element, ICollection<string> textParts)
    {
        if (ReferenceEquals(element, HelpStatusLabel))
        {
            return;
        }

        if (element is Label { Text: { } text } && !string.IsNullOrWhiteSpace(text))
        {
            textParts.Add(text.Trim());
        }

        if (element is Border { Content: Element borderContent })
        {
            CollectLabelText(borderContent, textParts);
            return;
        }

        if (element is ScrollView { Content: Element scrollContent })
        {
            CollectLabelText(scrollContent, textParts);
            return;
        }

        if (element is Layout layout)
        {
            foreach (var child in layout.Children)
            {
                if (child is Element childElement)
                {
                    CollectLabelText(childElement, textParts);
                }
            }
        }
    }
}
