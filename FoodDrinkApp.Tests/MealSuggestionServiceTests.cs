using FoodDrinkApp.Models;
using FoodDrinkApp.Services;

namespace FoodDrinkApp.Tests;

public sealed class MealSuggestionServiceTests
{
    [Fact]
    public void PickRandom_returns_deterministic_in_range_item_for_seeded_random()
    {
        FoodItem[] items =
        [
            new() { Name = "Tea" },
            new() { Name = "Toast" },
            new() { Name = "Soup" }
        ];

        var first = MealSuggestionService.PickRandom(items, new Random(8));
        var second = MealSuggestionService.PickRandom(items, new Random(8));

        Assert.Same(first, items.Single(item => item.Name == first.Name));
        Assert.Equal(first.Name, second.Name);
        Assert.Contains(first, items);
    }

    [Fact]
    public void PickRandom_rejects_empty_catalog()
    {
        Assert.Throws<ArgumentException>(() => MealSuggestionService.PickRandom([], new Random(1)));
    }
}
