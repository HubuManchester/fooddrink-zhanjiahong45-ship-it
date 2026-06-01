using FoodDrinkApp.Models;

namespace FoodDrinkApp.Tests;

public sealed class FoodItemTests
{
    [Fact]
    public void Formats_display_summaries()
    {
        var item = new FoodItem
        {
            Name = "Green Tea",
            Category = "Drink",
            Calories = 4,
            Protein = 0,
            Carbs = 1,
            Fat = 0,
            AllergyNote = "No common allergens recorded."
        };

        Assert.Equal("4 kcal", item.CaloriesLabel);
        Assert.Equal("Protein 0g, carbs 1g, fat 0g", item.MacroSummary);
        Assert.Equal(
            "Green Tea. Drink. 4 kcal. Protein 0g, carbs 1g, fat 0g. No common allergens recorded.",
            item.AccessibleSummary);
    }
}
