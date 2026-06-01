using FoodDrinkApp.Models;
using FoodDrinkApp.Services;

namespace FoodDrinkApp.Tests;

public sealed class FoodFilterServiceTests
{
    [Fact]
    public void Apply_filters_by_category_case_insensitively()
    {
        var result = FoodFilterService.Apply(SampleItems(), "drink", new HashSet<string>(), favoritesOnly: false);

        Assert.Equal(["2"], result.Select(item => item.Id));
    }

    [Fact]
    public void Apply_filters_by_favorites()
    {
        var result = FoodFilterService.Apply(SampleItems(), null, new HashSet<string> { "1", "3" }, favoritesOnly: true);

        Assert.Equal(["1", "3"], result.Select(item => item.Id));
    }

    [Fact]
    public void Apply_combines_category_and_favorites()
    {
        var result = FoodFilterService.Apply(SampleItems(), "meal", new HashSet<string> { "2", "3" }, favoritesOnly: true);

        Assert.Equal(["3"], result.Select(item => item.Id));
    }

    private static FoodItem[] SampleItems() =>
    [
        new() { Id = "1", Name = "Berry Yogurt", Category = "Breakfast" },
        new() { Id = "2", Name = "Iced Matcha", Category = "Drink" },
        new() { Id = "3", Name = "Rice Box", Category = "Meal" }
    ];
}
