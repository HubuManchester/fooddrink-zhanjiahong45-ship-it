using FoodDrinkApp.Models;

namespace FoodDrinkApp.Services;

public static class MealSuggestionService
{
    public static FoodItem PickRandom(IReadOnlyList<FoodItem> items, Random rng)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(rng);

        if (items.Count == 0)
        {
            throw new ArgumentException("At least one food item is required.", nameof(items));
        }

        return items[rng.Next(items.Count)];
    }
}
