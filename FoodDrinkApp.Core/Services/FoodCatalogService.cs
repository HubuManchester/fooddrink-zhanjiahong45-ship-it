using System.Net.Http.Json;
using System.Text.Json;
using FoodDrinkApp.Models;

namespace FoodDrinkApp.Services;

/// <summary>
/// Loads, searches, and creates food records using a remote HTTPS catalogue with local fallback data.
/// </summary>
public static class FoodCatalogService
{
    private static readonly TimeSpan RemoteCatalogTimeout = TimeSpan.FromSeconds(6);

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = RemoteCatalogTimeout
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
            AllergyNote = "Contains dairy and gluten",
            Tags = "high-protein, vegetarian, breakfast, berries",
            IsFavorite = true
        },
        new()
        {
            Id = "2",
            Name = "Avocado Egg Toast",
            Category = "Breakfast",
            Description = "Wholegrain toast topped with smashed avocado, boiled egg, chilli flakes, and lemon.",
            Calories = 420,
            Protein = 18,
            Carbs = 38,
            Fat = 23,
            AllergyNote = "Contains egg and gluten",
            Tags = "breakfast, vegetarian, eggs, wholegrain"
        },
        new()
        {
            Id = "3",
            Name = "Overnight Oats Jar",
            Category = "Breakfast",
            Description = "Rolled oats soaked with chia seeds, milk, apple, cinnamon, and pumpkin seeds.",
            Calories = 390,
            Protein = 15,
            Carbs = 56,
            Fat = 12,
            AllergyNote = "Contains dairy and gluten",
            Tags = "breakfast, oats, fibre, vegetarian"
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
            AllergyNote = "No common allergens recorded",
            Tags = "meal-prep, lunch, high-protein, chicken",
            IsFavorite = true
        },
        new()
        {
            Id = "5",
            Name = "Tuna Sweetcorn Wrap",
            Category = "Lunch",
            Description = "Wholemeal wrap with tuna, sweetcorn, lettuce, cucumber, and light yoghurt dressing.",
            Calories = 455,
            Protein = 31,
            Carbs = 47,
            Fat = 15,
            AllergyNote = "Contains fish, dairy, and gluten",
            Tags = "lunch, tuna, wrap, high-protein"
        },
        new()
        {
            Id = "6",
            Name = "Lentil Soup and Rye Roll",
            Category = "Lunch",
            Description = "Tomato lentil soup served with a small rye roll and mixed leaves.",
            Calories = 430,
            Protein = 22,
            Carbs = 64,
            Fat = 9,
            AllergyNote = "Contains gluten",
            Tags = "lunch, vegetarian, lentils, soup"
        },
        new()
        {
            Id = "7",
            Name = "Salmon Quinoa Plate",
            Category = "Dinner",
            Description = "Baked salmon with quinoa, broccoli, peas, and yoghurt herb sauce.",
            Calories = 640,
            Protein = 42,
            Carbs = 50,
            Fat = 28,
            AllergyNote = "Contains fish and dairy",
            Tags = "dinner, salmon, omega-3, high-protein",
            IsFavorite = true
        },
        new()
        {
            Id = "8",
            Name = "Tomato Wholegrain Pasta",
            Category = "Dinner",
            Description = "Wholegrain pasta with tomato sauce, basil, courgette, peppers, and grated cheese.",
            Calories = 610,
            Protein = 18,
            Carbs = 92,
            Fat = 16,
            AllergyNote = "Contains gluten and dairy",
            Tags = "dinner, vegetarian, pasta, vegetables"
        },
        new()
        {
            Id = "9",
            Name = "Turkey Chilli Bowl",
            Category = "Dinner",
            Description = "Lean turkey chilli with kidney beans, tomatoes, brown rice, and coriander.",
            Calories = 585,
            Protein = 41,
            Carbs = 63,
            Fat = 17,
            AllergyNote = "No common allergens recorded",
            Tags = "dinner, turkey, beans, spicy"
        },
        new()
        {
            Id = "10",
            Name = "Hummus Veg Snack Pot",
            Category = "Snack",
            Description = "Carrot sticks, cucumber, cherry tomatoes, and hummus in a snack pot.",
            Calories = 220,
            Protein = 8,
            Carbs = 24,
            Fat = 10,
            AllergyNote = "Contains sesame",
            Tags = "snack, vegan, vegetables, hummus",
            IsFavorite = true
        },
        new()
        {
            Id = "11",
            Name = "Apple Peanut Butter Slices",
            Category = "Snack",
            Description = "Fresh apple slices with a measured serving of smooth peanut butter.",
            Calories = 260,
            Protein = 7,
            Carbs = 31,
            Fat = 13,
            AllergyNote = "Contains peanuts",
            Tags = "snack, fruit, nuts, fibre"
        },
        new()
        {
            Id = "12",
            Name = "Protein Trail Mix",
            Category = "Snack",
            Description = "A portion-controlled mix of almonds, pumpkin seeds, raisins, and dark chocolate.",
            Calories = 310,
            Protein = 11,
            Carbs = 28,
            Fat = 18,
            AllergyNote = "Contains tree nuts",
            Tags = "snack, nuts, seeds, energy"
        },
        new()
        {
            Id = "13",
            Name = "Iced Matcha Latte",
            Category = "Drink",
            Description = "Matcha, milk, and ice with a lower-sugar recipe for steady energy.",
            Calories = 180,
            Protein = 8,
            Carbs = 22,
            Fat = 6,
            AllergyNote = "Contains dairy unless plant-based milk is selected",
            Tags = "drink, caffeine, matcha, latte",
            IsFavorite = true
        },
        new()
        {
            Id = "14",
            Name = "Citrus Mint Infused Water",
            Category = "Drink",
            Description = "Still water infused with orange, lemon, cucumber, and fresh mint.",
            Calories = 25,
            Protein = 0,
            Carbs = 6,
            Fat = 0,
            AllergyNote = "No common allergens recorded",
            Tags = "drink, hydration, low-calorie, citrus"
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

        var remoteItems = await TryLoadAnyRemoteCatalogAsync(GetReadOnlyCatalogEndpoints().ToList());
        if (remoteItems.Count > 0)
        {
            cachedItems = remoteItems.Select(CloneCatalogItem).ToList();
            LastLoadUsedRemote = true;
            LastLoadedCatalogCount = cachedItems.Count;
            return cachedItems;
        }

        return UseLocalFallback();
    }

    private static IEnumerable<string> GetReadOnlyCatalogEndpoints()
    {
        foreach (var mirrorUrl in MockApiConfig.ReadOnlyMirrorUrls)
        {
            if (!string.IsNullOrWhiteSpace(mirrorUrl) &&
                !string.Equals(mirrorUrl, MockApiConfig.EndpointUrl, StringComparison.OrdinalIgnoreCase))
            {
                yield return mirrorUrl;
            }
        }

        yield return MockApiConfig.EndpointUrl;
    }

    private static async Task<IReadOnlyList<FoodItem>> TryLoadAnyRemoteCatalogAsync(IReadOnlyList<string> endpointUrls)
    {
        using var timeoutSource = new CancellationTokenSource(RemoteCatalogTimeout);
        var pendingTasks = endpointUrls
            .Select(endpointUrl => TryLoadRemoteCatalogAsync(endpointUrl, timeoutSource.Token))
            .ToList();

        while (pendingTasks.Count > 0)
        {
            var completedTask = await Task.WhenAny(pendingTasks);
            pendingTasks.Remove(completedTask);

            var items = await completedTask;
            if (items.Count > 0)
            {
                await timeoutSource.CancelAsync();
                return items;
            }
        }

        if (timeoutSource.IsCancellationRequested)
        {
            AppLog.Error(
                "Load food catalog from remote endpoints timed out",
                new TaskCanceledException($"No configured catalogue endpoint responded within {RemoteCatalogTimeout.TotalSeconds:0} seconds."));
        }

        return [];
    }

    private static async Task<IReadOnlyList<FoodItem>> TryLoadRemoteCatalogAsync(
        string endpointUrl,
        CancellationToken cancellationToken)
    {
        try
        {
            var json = await HttpClient.GetStringAsync(endpointUrl, cancellationToken);
            var items = DeserializeCatalogJson(json);
            if (items.Count > 0)
            {
                return items;
            }

            AppLog.Error(
                $"Parse food catalog from remote endpoint {endpointUrl}",
                new JsonException("The remote catalogue did not contain any food items."));
        }
        catch (HttpRequestException ex)
        {
            AppLog.Error($"Load food catalog from remote endpoint {endpointUrl}", ex);
        }
        catch (TaskCanceledException ex)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                AppLog.Error($"Load food catalog from remote endpoint timed out {endpointUrl}", ex);
            }
        }
        catch (JsonException ex)
        {
            AppLog.Error($"Parse food catalog from remote endpoint {endpointUrl}", ex);
        }
        catch (Exception ex)
        {
            AppLog.Error($"Load food catalog from remote endpoint {endpointUrl}", ex);
            // Keep the app usable during demos even if the network is unavailable.
        }

        return [];
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
