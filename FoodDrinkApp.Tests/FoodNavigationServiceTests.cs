using FoodDrinkApp.Services;

namespace FoodDrinkApp.Tests;

public sealed class FoodNavigationServiceTests
{
    [Fact]
    public void GetAdjacentId_returns_next_and_previous_ids()
    {
        var ids = new[] { "breakfast", "lunch", "dinner" };

        Assert.Equal("dinner", FoodNavigationService.GetAdjacentId(ids, "lunch", 1));
        Assert.Equal("breakfast", FoodNavigationService.GetAdjacentId(ids, "lunch", -1));
    }

    [Fact]
    public void GetAdjacentId_wraps_at_list_edges_by_default()
    {
        var ids = new[] { "breakfast", "lunch", "dinner" };

        Assert.Equal("breakfast", FoodNavigationService.GetAdjacentId(ids, "dinner", 1));
        Assert.Equal("dinner", FoodNavigationService.GetAdjacentId(ids, "breakfast", -1));
    }

    [Fact]
    public void GetAdjacentId_can_stop_at_list_edges()
    {
        var ids = new[] { "breakfast", "lunch", "dinner" };

        Assert.Null(FoodNavigationService.GetAdjacentId(ids, "dinner", 1, wrap: false));
        Assert.Null(FoodNavigationService.GetAdjacentId(ids, "breakfast", -1, wrap: false));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("")]
    public void GetAdjacentId_returns_null_when_current_id_is_unusable(string currentId)
    {
        var ids = new[] { "breakfast", "lunch", "dinner" };

        Assert.Null(FoodNavigationService.GetAdjacentId(ids, currentId, 1));
    }
}
