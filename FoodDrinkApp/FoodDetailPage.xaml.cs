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
            FavoriteButton.IsEnabled = false;
            DeleteButton.IsEnabled = false;
            SetDetailStatus("Record not found.", announce: false);
            return;
        }

        FavoriteButton.IsEnabled = true;
        DeleteButton.IsEnabled = true;
        NameLabel.Text = currentItem.Name;
        CategoryLabel.Text = currentItem.Category;
        CaloriesLabel.Text = currentItem.CaloriesLabel;
        MacroLabel.Text = currentItem.MacroSummary;
        DescriptionLabel.Text = currentItem.Description;
        AllergyLabel.Text = currentItem.AllergyNote;
        macroRingDrawable.SetMacros(currentItem.Protein, currentItem.Carbs, currentItem.Fat);
        AnimateMacroRing();
        SemanticProperties.SetDescription(NameLabel, currentItem.AccessibleSummary);
        SemanticProperties.SetDescription(MacroRingView, $"Macro ratio ring for {currentItem.MacroSummary}.");
        UpdateFavoriteButton();
        SetDetailStatus("Ready.", announce: false);
    }

    private async Task AnimateEntranceAsync()
    {
        var views = new View[] { DetailHero, MacroCard, ActionPanel, RecordActionsPanel };

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

    private async void OnFavoriteClicked(object? sender, EventArgs e)
    {
        if (currentItem is null)
        {
            await DisplayAlert("Missing record", "There is no record to favourite.", "OK");
            return;
        }

        var newFavoriteState = !currentItem.IsFavorite;
        currentItem.IsFavorite = newFavoriteState;
        SetRecordActionsEnabled(false);
        UpdateFavoriteButton();

        try
        {
            var repository = await AppDataService.GetRepositoryAsync();
            await repository.UpdateAsync(currentItem);
            SetDetailStatus(newFavoriteState
                ? $"{currentItem.Name} added to favourites."
                : $"{currentItem.Name} removed from favourites.");
        }
        catch (Exception ex)
        {
            currentItem.IsFavorite = !newFavoriteState;
            AppLog.Error("Update favourite state from detail page", ex);
            SetDetailStatus("The favourite state could not be saved right now.");
            UpdateFavoriteButton();
        }
        finally
        {
            SetRecordActionsEnabled(true);
        }
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (currentItem is null)
        {
            await DisplayAlert("Missing record", "There is no record to delete.", "OK");
            return;
        }

        var itemName = currentItem.Name;
        var confirmed = await DisplayAlert("Delete record", $"Delete {itemName}?", "Delete", "Cancel");
        if (!confirmed)
        {
            return;
        }

        SetRecordActionsEnabled(false);
        try
        {
            var repository = await AppDataService.GetRepositoryAsync();
            await repository.DeleteAsync(currentItem);
            SetDetailStatus($"{itemName} deleted.");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            AppLog.Error("Delete food record from detail page", ex);
            SetDetailStatus("The record could not be deleted right now.");
            SetRecordActionsEnabled(true);
        }
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

    private void UpdateFavoriteButton()
    {
        if (currentItem is null)
        {
            FavoriteButton.Text = "Add to favourites";
            SemanticProperties.SetDescription(FavoriteButton, "Add to favourites");
            SemanticProperties.SetHint(FavoriteButton, "Load a record before changing favourite state");
            return;
        }

        if (currentItem.IsFavorite)
        {
            FavoriteButton.Text = "Favourited";
            SemanticProperties.SetDescription(FavoriteButton, $"{currentItem.Name} is favourited");
            SemanticProperties.SetHint(FavoriteButton, "Remove this food or drink record from favourites");
            return;
        }

        FavoriteButton.Text = "Add to favourites";
        SemanticProperties.SetDescription(FavoriteButton, $"{currentItem.Name} is not favourited");
        SemanticProperties.SetHint(FavoriteButton, "Add this food or drink record to favourites");
    }

    private void SetRecordActionsEnabled(bool isEnabled)
    {
        FavoriteButton.IsEnabled = isEnabled && currentItem is not null;
        DeleteButton.IsEnabled = isEnabled && currentItem is not null;
    }

    private void SetDetailStatus(string message, bool announce = true)
    {
        DetailStatusLabel.Text = message;

        if (announce)
        {
            SemanticScreenReader.Announce(message);
        }
    }
}
