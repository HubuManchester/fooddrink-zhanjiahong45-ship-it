using FoodDrinkApp.Models;

namespace FoodDrinkApp.Services;

/// <summary>
/// Applies category and favorite filters to a food list.
/// </summary>
public static class FoodFilterService
{
    /// <summary>
    /// Applies the selected category and favourite-only state to an existing item list.
    /// </summary>
    public static IReadOnlyList<FoodItem> Apply(
        IReadOnlyList<FoodItem> items,
        string? category,
        IReadOnlySet<string> favoriteIds,
        bool favoritesOnly)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(favoriteIds);

        IEnumerable<FoodItem> filtered = items;

        if (!string.IsNullOrWhiteSpace(category))
        {
            filtered = filtered.Where(item => string.Equals(item.Category, category, StringComparison.OrdinalIgnoreCase));
        }

        if (favoritesOnly)
        {
            filtered = filtered.Where(item => favoriteIds.Contains(item.Id));
        }

        return filtered.ToArray();
    }
}
