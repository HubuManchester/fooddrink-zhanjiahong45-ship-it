using FoodDrinkApp.Models;
using FoodDrinkApp.Services;

namespace FoodDrinkApp;

[QueryProperty(nameof(ItemId), "id")]
public partial class AddItemPage : ContentPage
{
    private FoodItem? editingItem;
    private string? pendingItemId;

    public AddItemPage()
    {
        InitializeComponent();
    }

    public string ItemId
    {
        set
        {
            pendingItemId = value;
            _ = LoadEditItemAsync(value);
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        AccessibilityService.ApplyFontScale(this);

        if (!string.IsNullOrWhiteSpace(pendingItemId) && editingItem is null)
        {
            await LoadEditItemAsync(pendingItemId);
        }
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        try
        {
            var validationMessage = ValidateForm(out var calories, out var protein, out var carbs, out var fat);
            if (validationMessage is not null)
            {
                ShowValidation(validationMessage);
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(250));
                return;
            }

            var item = BuildFoodItem(calories, protein, carbs, fat);
            var repository = await AppDataService.GetRepositoryAsync();

            if (editingItem is null)
            {
                await repository.AddAsync(item);
            }
            else
            {
                item.LocalId = editingItem.LocalId;
                item.Id = editingItem.Id;
                await repository.UpdateAsync(item);
            }

            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            SemanticScreenReader.Announce(editingItem is null ? "Food record saved." : "Food record updated.");

            await DisplayAlert(
                editingItem is null ? "Saved" : "Updated",
                editingItem is null
                    ? "The record has been saved to the local database."
                    : "The record has been updated in the local database.",
                "OK");

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            AppLog.Error("Save food record", ex);
            ShowValidation("The record could not be saved right now. Please check your connection and try again.");
        }
    }

    private async Task LoadEditItemAsync(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        try
        {
            var repository = await AppDataService.GetRepositoryAsync();
            editingItem = await repository.GetByIdAsync(id);
            if (editingItem is null)
            {
                ShowValidation("The selected record could not be loaded for editing.");
                return;
            }

            Title = "Edit Record";
            FormTitleLabel.Text = "Edit food or drink";
            FormSubtitleLabel.Text = "Update the saved nutrition details.";
            SaveButton.Text = "Update record";

            NameEntry.Text = editingItem.Name;
            SelectCategory(editingItem.Category);
            DescriptionEditor.Text = editingItem.Description;
            CaloriesEntry.Text = editingItem.Calories.ToString();
            ProteinEntry.Text = editingItem.Protein.ToString();
            CarbsEntry.Text = editingItem.Carbs.ToString();
            FatEntry.Text = editingItem.Fat.ToString();
            AllergyEntry.Text = editingItem.AllergyNote;
            ValidationPanel.IsVisible = false;
        }
        catch (Exception ex)
        {
            AppLog.Error("Load food record for editing", ex);
            ShowValidation("The selected record could not be loaded for editing.");
        }
    }

    private FoodItem BuildFoodItem(int calories, int protein, int carbs, int fat) =>
        new()
        {
            Name = NameEntry.Text!.Trim(),
            Category = CategoryPicker.SelectedItem?.ToString() ?? "Snack",
            Description = DescriptionEditor.Text!.Trim(),
            Calories = calories,
            Protein = protein,
            Carbs = carbs,
            Fat = fat,
            AllergyNote = string.IsNullOrWhiteSpace(AllergyEntry.Text)
                ? "No allergy note provided."
                : AllergyEntry.Text.Trim(),
            Tags = $"{NameEntry.Text} {CategoryPicker.SelectedItem} {DescriptionEditor.Text}",
            IsFavorite = editingItem?.IsFavorite ?? false
        };

    private void SelectCategory(string category)
    {
        var index = CategoryPicker.Items.IndexOf(category);
        if (index < 0)
        {
            CategoryPicker.Items.Add(category);
            index = CategoryPicker.Items.Count - 1;
        }

        CategoryPicker.SelectedIndex = index;
    }

    private string? ValidateForm(out int calories, out int protein, out int carbs, out int fat)
    {
        calories = protein = carbs = fat = 0;

        var validation = FoodItemValidator.Validate(
            NameEntry.Text,
            CategoryPicker.SelectedIndex,
            DescriptionEditor.Text,
            CaloriesEntry.Text,
            ProteinEntry.Text,
            CarbsEntry.Text,
            FatEntry.Text);

        if (!validation.IsValid)
        {
            return validation.Message;
        }

        calories = int.Parse(CaloriesEntry.Text!);
        protein = int.Parse(ProteinEntry.Text!);
        carbs = int.Parse(CarbsEntry.Text!);
        fat = int.Parse(FatEntry.Text!);
        return null;
    }

    private void ShowValidation(string message)
    {
        ValidationLabel.Text = message;
        ValidationPanel.IsVisible = true;
        SemanticScreenReader.Announce(message);
    }
}
