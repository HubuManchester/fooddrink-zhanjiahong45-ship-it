using System.Net.Http.Json;
using System.Text.Json;
using FoodDrinkApp.Models;

namespace FoodDrinkApp.Services;

/// <summary>
/// Loads, searches, and creates food records using a remote HTTPS catalogue with local fallback data.
/// </summary>
public static class FoodCatalogService
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(12)
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly List<FoodItem> LocalFallbackItems =
    [
        new()
        {
            Id = "1",
            Name = "Berry Yogurt Bowl",
            Category = "Breakfast",
            Description = "Greek yogurt with mixed berries, oats, and a small drizzle of honey.",
            Calories = 340,
            Protein = 24,
            Carbs = 42,
            Fat = 8,
            AllergyNote = "Contains dairy and gluten.",
            Tags = "healthy breakfast yogurt berries",
            IsFavorite = true
        },
        new()
        {
            Id = "4",
            Name = "Chicken Brown Rice Box",
            Category = "Lunch",
            Description = "Grilled chicken breast with brown rice, spinach, cucumber, and lemon dressing.",
            Calories = 520,
            Protein = 38,
            Carbs = 58,
            Fat = 14,
            AllergyNote = "No common allergens recorded.",
            Tags = "meal prep protein lunch",
            IsFavorite = true
        },
        new()
        {
            Id = "13",
            Name = "Iced Matcha Latte",
            Category = "Drink",
            Description = "Matcha, milk, and ice. A lower-sugar version is recommended.",
            Calories = 180,
            Protein = 8,
            Carbs = 22,
            Fat = 6,
            AllergyNote = "Contains dairy unless plant-based milk is selected.",
            Tags = "drink caffeine matcha latte",
            IsFavorite = true
        },
        new()
        {
            Id = "8",
            Name = "Tomato Wholegrain Pasta",
            Category = "Dinner",
            Description = "Wholegrain pasta with tomato sauce, basil, and roasted vegetables.",
            Calories = 610,
            Protein = 18,
            Carbs = 92,
            Fat = 16,
            AllergyNote = "Contains gluten.",
            Tags = "vegetarian dinner pasta"
        }
    ];

    private static List<FoodItem> cachedItems = CreateLocalFallbackItems();

    /// <summary>
    /// Gets whether the most recent catalogue load used the configured remote endpoint.
    /// </summary>
    public static bool LastLoadUsedRemote { get; private set; }

    /// <summary>
    /// Gets whether the most recent catalogue load used the remote endpoint.
    /// </summary>
    public static bool LastLoadUsedMockApi => LastLoadUsedRemote;

    /// <summary>
    /// Gets the number of source items returned by the most recent catalogue load.
    /// </summary>
    public static int LastLoadedCatalogCount { get; private set; } = cachedItems.Count;

    /// <summary>
    /// Deserializes the remote catalogue JSON shape into food records.
    /// </summary>
    public static IReadOnlyList<FoodItem> DeserializeCatalogJson(string json) =>
        JsonSerializer.Deserialize<List<FoodItem>>(json, JsonOptions) ?? [];

    /// <summary>
    /// Searches the available catalogue by name, category, description, or tags.
    /// </summary>
    public static async Task<IReadOnlyList<FoodItem>> SearchAsync(string? query)
    {
        var items = await GetAllAsync();

        if (string.IsNullOrWhiteSpace(query))
        {
            return items.OrderBy(item => item.Name).ToList();
        }

        var normalised = query.Trim();
        return items
            .Where(item =>
                item.Name.Contains(normalised, StringComparison.OrdinalIgnoreCase) ||
                item.Category.Contains(normalised, StringComparison.OrdinalIgnoreCase) ||
                item.Description.Contains(normalised, StringComparison.OrdinalIgnoreCase) ||
                item.Tags.Contains(normalised, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.Name)
            .ToList();
    }

    /// <summary>
    /// Gets a single food item from a REST endpoint when available, falling back to the loaded catalogue.
    /// </summary>
    public static async Task<FoodItem?> GetByIdAsync(string id)
    {
        if (MockApiConfig.SupportsItemEndpoint)
        {
            try
            {
                var item = await HttpClient.GetFromJsonAsync<FoodItem>(
                    $"{MockApiConfig.EndpointUrl.TrimEnd('/')}/{Uri.EscapeDataString(id)}",
                    JsonOptions);

                if (item is not null)
                {
                    return item;
                }
            }
            catch (HttpRequestException ex)
            {
                AppLog.Error("Load food item from remote endpoint", ex);
            }
            catch (TaskCanceledException ex)
            {
                AppLog.Error("Load food item from remote endpoint timed out", ex);
            }
            catch (JsonException ex)
            {
                AppLog.Error("Parse food item from remote endpoint", ex);
            }
            catch (Exception ex)
            {
                AppLog.Error("Load food item from remote endpoint", ex);
            }
        }

        var items = await GetAllAsync();
        return items.FirstOrDefault(item => item.Id == id);
    }

    /// <summary>
    /// Adds a new food item to a writable REST endpoint when configured or to the local cache otherwise.
    /// </summary>
    public static async Task<FoodItem> AddAsync(FoodItem item)
    {
        if (MockApiConfig.SupportsItemEndpoint)
        {
            var response = await HttpClient.PostAsJsonAsync(MockApiConfig.EndpointUrl, item, JsonOptions);
            response.EnsureSuccessStatusCode();

            var created = await response.Content.ReadFromJsonAsync<FoodItem>(JsonOptions);
            if (created is not null)
            {
                UpsertCachedItem(created);
                return created;
            }
        }

        UpsertCachedItem(item);
        return item;
    }

    private static async Task<IReadOnlyList<FoodItem>> GetAllAsync()
    {
        if (!MockApiConfig.IsConfigured)
        {
            return UseLocalFallback();
        }

        try
        {
            var json = await HttpClient.GetStringAsync(MockApiConfig.EndpointUrl);
            var items = DeserializeCatalogJson(json);
            if (items.Count > 0)
            {
                cachedItems = items.Select(CloneCatalogItem).ToList();
                LastLoadUsedRemote = true;
                LastLoadedCatalogCount = cachedItems.Count;
                return cachedItems;
            }

            AppLog.Error(
                "Parse food catalog from remote endpoint",
                new JsonException("The remote catalogue did not contain any food items."));
        }
        catch (HttpRequestException ex)
        {
            AppLog.Error("Load food catalog from remote endpoint", ex);
        }
        catch (TaskCanceledException ex)
        {
            AppLog.Error("Load food catalog from remote endpoint timed out", ex);
        }
        catch (JsonException ex)
        {
            AppLog.Error("Parse food catalog from remote endpoint", ex);
        }
        catch (Exception ex)
        {
            AppLog.Error("Load food catalog from remote endpoint", ex);
            // Keep the app usable during demos even if the network is unavailable.
        }

        return UseLocalFallback();
    }

    private static IReadOnlyList<FoodItem> UseLocalFallback()
    {
        cachedItems = CreateLocalFallbackItems();
        LastLoadUsedRemote = false;
        LastLoadedCatalogCount = cachedItems.Count;
        return cachedItems;
    }

    private static List<FoodItem> CreateLocalFallbackItems() =>
        LocalFallbackItems.Select(CloneCatalogItem).ToList();

    private static void UpsertCachedItem(FoodItem item)
    {
        cachedItems.RemoveAll(existing => existing.Id == item.Id);
        cachedItems.Add(CloneCatalogItem(item));
    }

    private static FoodItem CloneCatalogItem(FoodItem item) =>
        new()
        {
            Id = item.Id,
            Name = item.Name,
            Category = item.Category,
            Description = item.Description,
            Calories = item.Calories,
            Protein = item.Protein,
            Carbs = item.Carbs,
            Fat = item.Fat,
            AllergyNote = item.AllergyNote,
            Tags = item.Tags,
            IsFavorite = item.IsFavorite
        };
}
