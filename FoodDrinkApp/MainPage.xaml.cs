using FoodDrinkApp.Models;
using FoodDrinkApp.Services;

namespace FoodDrinkApp;

public partial class MainPage : ContentPage
{
    private const string AllCategories = "All categories";
    private readonly HashSet<string> favoriteItemIds = new(StringComparer.Ordinal);
    private IReadOnlyList<FoodItem> loadedItems = [];
    private CancellationTokenSource? searchDebounce;
    private int loadRequestVersion;
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

    protected override void OnDisappearing()
    {
        searchDebounce?.Cancel();
        base.OnDisappearing();
    }

    private async Task LoadFoodItemsAsync(string? query = null, CancellationToken cancellationToken = default)
    {
        var requestVersion = Interlocked.Increment(ref loadRequestVersion);
        try
        {
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            UpdateStatus("Loading foods...");

            var repository = await AppDataService.GetRepositoryAsync();
            var items = await repository.SearchAsync(query);
            cancellationToken.ThrowIfCancellationRequested();

            if (requestVersion != loadRequestVersion)
            {
                return;
            }

            loadedItems = items;
            SyncFavoriteIds(loadedItems);
            UpdateCategoryOptions(loadedItems);
            ApplyFilters();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            AppLog.Error("Load food list", ex);
            loadedItems = [];
            FoodCollection.ItemsSource = Array.Empty<FoodItem>();
            UpdateStatus("Food list could not be loaded. Local fallback data will be used on refresh.", announce: true);
        }
        finally
        {
            if (requestVersion == loadRequestVersion)
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
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
        searchDebounce?.Cancel();
        searchDebounce?.Dispose();
        searchDebounce = new CancellationTokenSource();
        var token = searchDebounce.Token;

        try
        {
            await Task.Delay(250, token);
            await LoadFoodItemsAsync(e.NewTextValue, token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async void OnSearchButtonPressed(object? sender, EventArgs e)
    {
        searchDebounce?.Cancel();
        await LoadFoodItemsAsync(SearchFoodBar.Text);
    }

    private async void OnDeleteInvoked(object? sender, EventArgs e)
    {
        if (sender is not SwipeItem swipeItem || swipeItem.CommandParameter is not string id)
        {
            return;
        }

        var item = loadedItems.FirstOrDefault(food => food.Id == id);
        var itemName = item?.Name ?? "this record";
        var confirmed = await DisplayAlert("Delete record", $"Delete {itemName}?", "Delete", "Cancel");
        if (!confirmed)
        {
            return;
        }

        try
        {
            var repository = await AppDataService.GetRepositoryAsync();
            var deleted = await repository.DeleteByIdAsync(id);
            favoriteItemIds.Remove(id);
            await LoadFoodItemsAsync(SearchFoodBar.Text);
            UpdateStatus(deleted ? $"{itemName} deleted." : $"{itemName} was already removed.", announce: true);
        }
        catch (Exception ex)
        {
            AppLog.Error("Delete local food record", ex);
            UpdateStatus("The record could not be deleted right now.", announce: true);
        }
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

    private async void OnFavoriteInvoked(object? sender, EventArgs e)
    {
        if (sender is not SwipeItem swipeItem || swipeItem.CommandParameter is not string id)
        {
            return;
        }

        var item = loadedItems.FirstOrDefault(food => food.Id == id);
        var itemName = item?.Name ?? "Item";
        var isFavorite = item is not null && !item.IsFavorite;

        if (item is not null)
        {
            item.IsFavorite = isFavorite;
        }

        _ = isFavorite ? favoriteItemIds.Add(id) : favoriteItemIds.Remove(id);

        ApplyFilters();

        try
        {
            if (item is not null)
            {
                var repository = await AppDataService.GetRepositoryAsync();
                await repository.UpdateAsync(item);
            }

            UpdateStatus(isFavorite
                ? $"{itemName} added to favorites."
                : $"{itemName} removed from favorites.", announce: true);
        }
        catch (Exception ex)
        {
            AppLog.Error("Update favourite state", ex);
            UpdateStatus("The favourite state could not be saved right now.", announce: true);
        }
    }

    private async void OnRefreshing(object? sender, EventArgs e)
    {
        var importResult = new CatalogImportResult(0, 0, false);
        try
        {
            importResult = await AppDataService.ImportCatalogAsync();
        }
        catch (Exception ex)
        {
            AppLog.Error("Import food catalog into local database", ex);
        }

        await LoadFoodItemsAsync(SearchFoodBar.Text);
        FoodRefreshView.IsRefreshing = false;
        UpdateStatus(importResult.UsedRemote
            ? $"Loaded {importResult.SourceItemCount} items from the online catalogue; {importResult.SyncedCount} records synced to local SQLite."
            : $"Online catalogue unavailable. Loaded {importResult.SourceItemCount} local fallback items; {importResult.SyncedCount} records synced to local SQLite.",
            announce: true);
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

        UpdateStatus($"{visibleItems.Count} shown from {loadedItems.Count} foods. Source: local SQLite database.");
    }

    private void SyncFavoriteIds(IEnumerable<FoodItem> items)
    {
        favoriteItemIds.Clear();
        foreach (var item in items.Where(item => item.IsFavorite))
        {
            favoriteItemIds.Add(item.Id);
        }
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
