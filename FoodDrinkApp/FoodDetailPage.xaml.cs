using FoodDrinkApp.Models;
using FoodDrinkApp.Services;

namespace FoodDrinkApp;

[QueryProperty(nameof(ItemId), "id")]
public partial class FoodDetailPage : ContentPage
{
    private readonly MacroRingDrawable macroRingDrawable = new();
    private FoodItem? currentItem;
    private string? currentItemId;

    public FoodDetailPage()
    {
        InitializeComponent();
        MacroRingView.Drawable = macroRingDrawable;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        AccessibilityService.ApplyFontScale(this);
        if (!string.IsNullOrWhiteSpace(currentItemId))
        {
            _ = LoadItemAsync(currentItemId);
        }

        _ = AnimateEntranceAsync();
    }

    protected override void OnDisappearing()
    {
        SpeechService.Stop();
        base.OnDisappearing();
    }

    public string ItemId
    {
        set
        {
            currentItemId = value;
            _ = LoadItemAsync(value);
        }
    }

    private async Task LoadItemAsync(string id)
    {
        var repository = await AppDataService.GetRepositoryAsync();
        currentItem = await repository.GetByIdAsync(id);
        BindingContext = currentItem;
        RenderItem();
    }

    private void RenderItem()
    {
        if (currentItem is null)
        {
            NameLabel.Text = "Record not found";
            DescriptionLabel.Text = "The selected food or drink could not be loaded.";
            return;
        }

        NameLabel.Text = currentItem.Name;
        CategoryLabel.Text = currentItem.Category;
        CaloriesLabel.Text = currentItem.CaloriesLabel;
        MacroLabel.Text = currentItem.MacroSummary;
        DescriptionLabel.Text = currentItem.Description;
        AllergyLabel.Text = currentItem.AllergyNote;
        macroRingDrawable.SetMacros(currentItem.Protein, currentItem.Carbs, currentItem.Fat);
        AnimateMacroRing();
        SemanticProperties.SetDescription(NameLabel, currentItem.AccessibleSummary);
    }

    private async Task AnimateEntranceAsync()
    {
        var views = new View[] { DetailHero, MacroCard, ActionPanel };

        foreach (var view in views)
        {
            view.Opacity = 0;
            view.TranslationY = 14;
        }

        foreach (var view in views)
        {
            _ = view.FadeTo(1, 260, Easing.CubicOut);
            _ = view.TranslateTo(0, 0, 260, Easing.CubicOut);
            await Task.Delay(70);
        }
    }

    private void AnimateMacroRing()
    {
        macroRingDrawable.Progress = 0f;
        MacroRingView.Invalidate();
        this.AbortAnimation(nameof(MacroRingDrawable));

        var animation = new Animation(value =>
        {
            macroRingDrawable.Progress = (float)value;
            MacroRingView.Invalidate();
        }, 0, 1, Easing.CubicOut);

        animation.Commit(this, nameof(MacroRingDrawable), 16, 760);
    }

    private async void OnSpeakClicked(object? sender, EventArgs e)
    {
        if (currentItem is null)
        {
            await DisplayAlert("Missing record", "There is no nutrition summary to read.", "OK");
            return;
        }

        try
        {
            await SpeechService.SpeakAsync(currentItem.AccessibleSummary);
        }
        catch (Exception ex)
        {
            AppLog.Error("Read nutrition summary aloud", ex);
            await DisplayAlert("Text to speech unavailable", "This device could not read the summary aloud right now.", "OK");
        }
    }

    private void OnStopSpeechClicked(object? sender, EventArgs e)
    {
        SpeechService.Stop();
        SemanticScreenReader.Announce("Reading stopped.");
    }

    private async void OnEditClicked(object? sender, EventArgs e)
    {
        if (currentItem is null)
        {
            await DisplayAlert("Missing record", "There is no record to edit.", "OK");
            return;
        }

        await Shell.Current.GoToAsync($"{nameof(AddItemPage)}?id={Uri.EscapeDataString(currentItem.Id)}");
    }

    private async void OnVibrateClicked(object? sender, EventArgs e)
    {
        try
        {
            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500));
            HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
            await DisplayAlert("Reminder", "Vibration feedback has been triggered.", "OK");
        }
        catch (Exception ex)
        {
            AppLog.Error("Trigger nutrition reminder vibration", ex);
            await DisplayAlert("Vibration unavailable", "This device could not trigger vibration feedback right now.", "OK");
        }
    }
}
