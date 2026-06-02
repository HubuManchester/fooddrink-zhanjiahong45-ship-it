using FoodDrinkApp.Models;
using FoodDrinkApp.Services;

namespace FoodDrinkApp.Tests;

public sealed class FoodFilterServiceTests
{
    [Fact]
    public void Apply_returns_all_items_when_no_filters_are_active()
    {
        var result = FoodFilterService.Apply(SampleItems(), null, favoritesOnly: false);

        Assert.Equal(["1", "2", "3"], result.Select(item => item.Id));
    }

    [Fact]
    public void Apply_filters_by_category_case_insensitively()
    {
        var result = FoodFilterService.Apply(SampleItems(), "drink", favoritesOnly: false);

        Assert.Equal(["2"], result.Select(item => item.Id));
    }

    [Fact]
    public void Apply_filters_by_favorite_state()
    {
        var result = FoodFilterService.Apply(SampleItems(), null, favoritesOnly: true);

        Assert.Equal(["1", "3"], result.Select(item => item.Id));
    }

    [Fact]
    public void Apply_keeps_favorite_and_regular_items_when_favorites_only_is_off()
    {
        var result = FoodFilterService.Apply(SampleItems(), null, favoritesOnly: false);

        Assert.Equal(["1", "2", "3"], result.Select(item => item.Id));
    }

    [Fact]
    public void Apply_combines_category_and_favorites()
    {
        var result = FoodFilterService.Apply(SampleItems(), "meal", favoritesOnly: true);

        Assert.Equal(["3"], result.Select(item => item.Id));
    }

    private static FoodItem[] SampleItems() =>
    [
        new() { Id = "1", Name = "Berry Yogurt", Category = "Breakfast", IsFavorite = true },
        new() { Id = "2", Name = "Iced Matcha", Category = "Drink" },
        new() { Id = "3", Name = "Rice Box", Category = "Meal", IsFavorite = true }
    ];
}
