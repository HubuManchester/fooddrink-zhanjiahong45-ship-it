using FoodDrinkApp.Models;
using FoodDrinkApp.Services;

namespace FoodDrinkApp;

public partial class MainPage : ContentPage
{
    private const string AllCategories = "All categories";
    private const int LongPressDelayMilliseconds = 650;
    private IReadOnlyList<FoodItem> loadedItems = [];
    private IReadOnlyList<FoodItem> visibleItems = [];
    private CatalogImportResult? lastCatalogImportResult;
    private int lastVisibleItemCount;
    private CancellationTokenSource? searchDebounce;
    private CancellationTokenSource? foodCardLongPress;
    private readonly HashSet<string> suppressedTapIds = new(StringComparer.Ordinal);
    private int loadRequestVersion;
    private bool navigatingToDetail;
    private bool updatingCategoryPicker;
    private bool showingFoodQuickActions;

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
        CancelFoodCardLongPress();
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
            if (suppressedTapIds.Remove(id))
            {
                return;
            }

            await OpenFoodDetailsAsync(id);
        }
    }

    private void OnFoodCardPointerPressed(object? sender, PointerEventArgs e)
    {
        if (sender is not PointerGestureRecognizer recognizer ||
            recognizer.PointerPressedCommandParameter is not string id ||
            string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        CancelFoodCardLongPress();
        foodCardLongPress = new CancellationTokenSource();
        _ = ShowFoodQuickActionsAfterHoldAsync(id, foodCardLongPress.Token);
    }

    private void OnFoodCardPointerReleased(object? sender, PointerEventArgs e)
    {
        CancelFoodCardLongPress();
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
            var navigationIds = visibleItems.Count > 0 ? visibleItems : loadedItems;
            var orderedIds = string.Join(
                ",",
                navigationIds.Select(item => item.Id).Where(itemId => !string.IsNullOrWhiteSpace(itemId)));
            var route = $"{nameof(FoodDetailPage)}?id={Uri.EscapeDataString(id)}";

            if (!string.IsNullOrWhiteSpace(orderedIds))
            {
                route += $"&ids={Uri.EscapeDataString(orderedIds)}";
            }

            await Shell.Current.GoToAsync(route);
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
        var filteredItems = FoodFilterService.Apply(loadedItems, category, FavoritesOnlySwitch.IsToggled);

        visibleItems = filteredItems;
        FoodCollection.ItemsSource = visibleItems;
        lastVisibleItemCount = visibleItems.Count;

        UpdateStatus(BuildFoodListStatus(includeOfflineFallback: lastCatalogImportResult is { UsedRemote: false }));
    }

    private async Task ShowFoodQuickActionsAfterHoldAsync(string id, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(LongPressDelayMilliseconds, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        suppressedTapIds.Add(id);
        _ = ClearSuppressedTapAsync(id);

        if (showingFoodQuickActions)
        {
            return;
        }

        showingFoodQuickActions = true;
        try
        {
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
            }
            catch (Exception ex)
            {
                AppLog.Error("Trigger long-press haptic feedback", ex);
            }

            await ShowFoodQuickActionsAsync(id);
        }
        finally
        {
            showingFoodQuickActions = false;
        }
    }

    private async Task ClearSuppressedTapAsync(string id)
    {
        await Task.Delay(1500);
        suppressedTapIds.Remove(id);
    }

    private async Task ShowFoodQuickActionsAsync(string id)
    {
        try
        {
            var repository = await AppDataService.GetRepositoryAsync();
            var item = loadedItems.FirstOrDefault(food => string.Equals(food.Id, id, StringComparison.Ordinal)) ??
                await repository.GetByIdAsync(id);

            if (item is null)
            {
                UpdateStatus("That food record is no longer available.", announce: true);
                return;
            }

            var choice = await DisplayActionSheet(item.Name, "Cancel", null, "Toggle favourite", "Delete");
            switch (choice)
            {
                case "Toggle favourite":
                    await ToggleFavoriteFromQuickActionsAsync(item);
                    break;
                case "Delete":
                    await DeleteFromQuickActionsAsync(item);
                    break;
            }
        }
        catch (Exception ex)
        {
            AppLog.Error("Open food quick actions", ex);
            UpdateStatus("Quick actions could not be opened right now.", announce: true);
        }
    }

    private async Task ToggleFavoriteFromQuickActionsAsync(FoodItem item)
    {
        try
        {
            var isFavorite = await FoodRecordActionService.ToggleFavoriteAsync(item);
            await LoadFoodItemsAsync(SearchFoodBar.Text);
            UpdateStatus(isFavorite
                ? $"{item.Name} added to favourites."
                : $"{item.Name} removed from favourites.", announce: true);
        }
        catch (Exception ex)
        {
            AppLog.Error("Toggle favourite from food card quick actions", ex);
            UpdateStatus("The favourite state could not be saved right now.", announce: true);
        }
    }

    private async Task DeleteFromQuickActionsAsync(FoodItem item)
    {
        var confirmed = await DisplayAlert("Delete record", $"Delete {item.Name}?", "Delete", "Cancel");
        if (!confirmed)
        {
            return;
        }

        try
        {
            await FoodRecordActionService.DeleteAsync(item);
            await LoadFoodItemsAsync(SearchFoodBar.Text);
            UpdateStatus($"{item.Name} deleted.", announce: true);
        }
        catch (Exception ex)
        {
            AppLog.Error("Delete food record from food card quick actions", ex);
            UpdateStatus("The record could not be deleted right now.", announce: true);
        }
    }

    private void CancelFoodCardLongPress()
    {
        foodCardLongPress?.Cancel();
        foodCardLongPress?.Dispose();
        foodCardLongPress = null;
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
