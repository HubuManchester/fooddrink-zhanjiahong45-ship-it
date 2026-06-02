using FoodDrinkApp.Services;

namespace FoodDrinkApp.Tests;

public sealed class FoodCatalogServiceTests
{
    [Fact]
    public async Task Empty_query_returns_catalog_items_sorted_by_name()
    {
        var items = await FoodCatalogService.SearchAsync("");

        Assert.True(items.Count >= 14);
        Assert.Equal(items.OrderBy(item => item.Name).Select(item => item.Name), items.Select(item => item.Name));
        Assert.Contains(items, item => item.Name == "Berry Yogurt Bowl");
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
    public async Task Configured_endpoint_loads_catalog_or_fallback()
    {
        var items = await FoodCatalogService.SearchAsync(null);

        Assert.True(MockApiConfig.IsConfigured);
        Assert.NotEmpty(items);
        Assert.True(FoodCatalogService.LastLoadedCatalogCount >= 14);
    }

    [Fact]
    public async Task Non_matching_query_returns_empty_list()
    {
        var items = await FoodCatalogService.SearchAsync("not-in-the-catalog");

        Assert.Empty(items);
    }

    [Fact]
    public void Food_catalog_json_deserializes_remote_shape()
    {
        const string json = """
            [
              {
                "id": "sample-1",
                "name": "Berry Yogurt Bowl",
                "category": "Breakfast",
                "description": "Greek yogurt with berries.",
                "calories": 340,
                "protein": 24,
                "carbs": 42,
                "fat": 8,
                "tags": "high-protein, vegetarian",
                "allergyNote": "Contains dairy",
                "isFavorite": true
              }
            ]
            """;

        var items = FoodCatalogService.DeserializeCatalogJson(json);

        var item = Assert.Single(items);
        Assert.Equal("sample-1", item.Id);
        Assert.Equal("Berry Yogurt Bowl", item.Name);
        Assert.Equal("Breakfast", item.Category);
        Assert.Equal(340, item.Calories);
        Assert.Equal(24, item.Protein);
        Assert.Equal(42, item.Carbs);
        Assert.Equal(8, item.Fat);
        Assert.Equal("Contains dairy", item.AllergyNote);
        Assert.Equal("high-protein, vegetarian", item.Tags);
        Assert.True(item.IsFavorite);
    }
}
