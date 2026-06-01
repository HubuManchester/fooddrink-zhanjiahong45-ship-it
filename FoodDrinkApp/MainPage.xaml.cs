using FoodDrinkApp.Models;
using FoodDrinkApp.Services;

namespace FoodDrinkApp;

public partial class MainPage : ContentPage
{
    private const string AllCategories = "All categories";
    private readonly HashSet<string> favoriteItemIds = new(StringComparer.Ordinal);
    private IReadOnlyList<FoodItem> loadedItems = [];
    private bool updatingCategoryPicker;

    public MainPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        AccessibilityService.ApplyFontScale(this);
        await LoadFoodItemsAsync(SearchFoodBar.Text);
    }

    private async Task LoadFoodItemsAsync(string? query = null)
    {
        try
        {
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            UpdateStatus("Loading foods...");

            loadedItems = await FoodCatalogService.SearchAsync(query);
            UpdateCategoryOptions(loadedItems);
            ApplyFilters();
        }
        catch
        {
            loadedItems = [];
            FoodCollection.ItemsSource = Array.Empty<FoodItem>();
            UpdateStatus("Food list could not be loaded. Local fallback data will be used on refresh.", announce: true);
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }

    private async void OnAddClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(AddItemPage));
    }

    private async void OnDetailsClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is string id)
        {
            await Shell.Current.GoToAsync($"{nameof(FoodDetailPage)}?id={Uri.EscapeDataString(id)}");
        }
    }

    private async void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        await LoadFoodItemsAsync(e.NewTextValue);
    }

    private async void OnSearchButtonPressed(object? sender, EventArgs e)
    {
        await LoadFoodItemsAsync(SearchFoodBar.Text);
    }

    private void OnCategoryChanged(object? sender, EventArgs e)
    {
        if (!updatingCategoryPicker)
        {
            ApplyFilters();
        }
    }

    private void OnFavoritesOnlyToggled(object? sender, ToggledEventArgs e)
    {
        ApplyFilters();
    }

    private void OnFavoriteInvoked(object? sender, EventArgs e)
    {
        if (sender is not SwipeItem swipeItem || swipeItem.CommandParameter is not string id)
        {
            return;
        }

        var item = loadedItems.FirstOrDefault(food => food.Id == id);
        var itemName = item?.Name ?? "Item";
        var isFavorite = favoriteItemIds.Add(id);

        if (!isFavorite)
        {
            favoriteItemIds.Remove(id);
        }

        ApplyFilters();
        UpdateStatus(isFavorite
            ? $"{itemName} added to favorites."
            : $"{itemName} removed from favorites.", announce: true);
    }

    private async void OnRefreshing(object? sender, EventArgs e)
    {
        await LoadFoodItemsAsync(SearchFoodBar.Text);
        FoodRefreshView.IsRefreshing = false;
        var source = FoodCatalogService.LastLoadUsedMockApi ? "mockapi.io" : "local fallback data";
        UpdateStatus($"Food and drink list refreshed. Current source: {source}.", announce: true);
    }

    private void UpdateCategoryOptions(IReadOnlyList<FoodItem> items)
    {
        var previous = CategoryPicker.SelectedItem as string ?? AllCategories;
        var categories = items
            .Select(item => item.Category)
            .Where(category => !string.IsNullOrWhiteSpace(category))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(category => category)
            .ToArray();

        updatingCategoryPicker = true;
        CategoryPicker.Items.Clear();
        CategoryPicker.Items.Add(AllCategories);

        foreach (var category in categories)
        {
            CategoryPicker.Items.Add(category);
        }

        var selectedIndex = CategoryPicker.Items.IndexOf(previous);
        CategoryPicker.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
        updatingCategoryPicker = false;
    }

    private void ApplyFilters()
    {
        var selectedCategory = CategoryPicker.SelectedItem as string;
        var category = selectedCategory == AllCategories ? null : selectedCategory;
        var visibleItems = FoodFilterService.Apply(loadedItems, category, favoriteItemIds, FavoritesOnlySwitch.IsToggled);

        FoodCollection.ItemsSource = visibleItems;

        var source = FoodCatalogService.LastLoadUsedMockApi ? "mockapi.io" : "local fallback";
        UpdateStatus($"{visibleItems.Count} shown from {loadedItems.Count} foods. Source: {source}.");
    }

    private void UpdateStatus(string message, bool announce = false)
    {
        ResultsStatusLabel.Text = message;

        if (announce)
        {
            SemanticScreenReader.Announce(message);
        }
    }
}
