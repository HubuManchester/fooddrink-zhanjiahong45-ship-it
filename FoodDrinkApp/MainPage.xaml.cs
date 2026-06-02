using FoodDrinkApp.Models;
using FoodDrinkApp.Services;

namespace FoodDrinkApp;

public partial class MainPage : ContentPage
{
    private const string AllCategories = "All categories";
    private IReadOnlyList<FoodItem> loadedItems = [];
    private CatalogImportResult? lastCatalogImportResult;
    private int lastVisibleItemCount;
    private CancellationTokenSource? searchDebounce;
    private int loadRequestVersion;
    private bool navigatingToDetail;
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
            lastCatalogImportResult = AppDataService.LastCatalogImportResult;
            var items = await repository.SearchAsync(query);
            cancellationToken.ThrowIfCancellationRequested();

            if (requestVersion != loadRequestVersion)
            {
                return;
            }

            loadedItems = items;
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
            await OpenFoodDetailsAsync(id);
        }
    }

    private async void OnFoodCardTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is string id)
        {
            await OpenFoodDetailsAsync(id);
        }
    }

    private async Task OpenFoodDetailsAsync(string id)
    {
        if (navigatingToDetail)
        {
            return;
        }

        navigatingToDetail = true;
        try
        {
            await Shell.Current.GoToAsync($"{nameof(FoodDetailPage)}?id={Uri.EscapeDataString(id)}");
        }
        finally
        {
            navigatingToDetail = false;
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

    private async void OnRefreshing(object? sender, EventArgs e)
    {
        var importResult = AppDataService.LastCatalogImportResult ?? new CatalogImportResult(0, 0, false, loadedItems.Count);
        try
        {
            importResult = await AppDataService.ImportCatalogAsync();
            lastCatalogImportResult = importResult;
        }
        catch (Exception ex)
        {
            AppLog.Error("Import food catalog into local database", ex);
            importResult = new CatalogImportResult(0, 0, false, loadedItems.Count);
            lastCatalogImportResult = importResult;
        }

        await LoadFoodItemsAsync(SearchFoodBar.Text);
        lastCatalogImportResult = importResult;
        FoodRefreshView.IsRefreshing = false;
        UpdateStatus(BuildFoodListStatus(includeOfflineFallback: !importResult.UsedRemote), announce: true);
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
        var visibleItems = FoodFilterService.Apply(loadedItems, category, FavoritesOnlySwitch.IsToggled);

        FoodCollection.ItemsSource = visibleItems;
        lastVisibleItemCount = visibleItems.Count;

        UpdateStatus(BuildFoodListStatus());
    }

    private string BuildFoodListStatus(bool includeOfflineFallback = false)
    {
        var source = lastCatalogImportResult?.UsedRemote == true
            ? "online catalogue"
            : "local SQLite database";
        var message = $"{lastVisibleItemCount} shown from {loadedItems.Count} foods. Source: {source}.";

        if (includeOfflineFallback)
        {
            message += " Online catalogue unavailable; showing saved local records.";
        }

        return message;
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
