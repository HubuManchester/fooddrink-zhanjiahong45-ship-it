using FoodDrinkApp.Services;

namespace FoodDrinkApp.Tests;

public sealed class FoodCatalogServiceTests
{
    [Fact]
    public async Task Empty_query_returns_local_items_sorted_by_name()
    {
        var items = await FoodCatalogService.SearchAsync("");

        Assert.Equal(
            ["Berry Yogurt Bowl", "Chicken Brown Rice Box", "Iced Matcha Latte", "Tomato Wholegrain Pasta"],
            items.Select(item => item.Name));
    }

    [Theory]
    [InlineData("berry", "Berry Yogurt Bowl")]
    [InlineData("BREAKFAST", "Berry Yogurt Bowl")]
    [InlineData("Greek yogurt", "Berry Yogurt Bowl")]
    [InlineData("protein", "Chicken Brown Rice Box")]
    [InlineData("drink", "Iced Matcha Latte")]
    [InlineData("vegetarian", "Tomato Wholegrain Pasta")]
    public async Task Query_matches_name_category_description_or_tags_case_insensitively(string query, string expectedName)
    {
        var items = await FoodCatalogService.SearchAsync(query);

        Assert.Contains(items, item => item.Name == expectedName);
    }

    [Fact]
    public async Task Query_trims_whitespace_before_matching()
    {
        var items = await FoodCatalogService.SearchAsync("  matcha  ");

        Assert.Single(items);
        Assert.Equal("Iced Matcha Latte", items[0].Name);
    }

    [Fact]
    public async Task Empty_endpoint_uses_local_fallback()
    {
        var items = await FoodCatalogService.SearchAsync(null);

        Assert.False(FoodCatalogService.LastLoadUsedMockApi);
        Assert.NotEmpty(items);
    }

    [Fact]
    public async Task Non_matching_query_returns_empty_list()
    {
        var items = await FoodCatalogService.SearchAsync("not-in-the-catalog");

        Assert.Empty(items);
    }
}
