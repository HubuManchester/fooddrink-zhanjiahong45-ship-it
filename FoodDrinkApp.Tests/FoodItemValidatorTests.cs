using FoodDrinkApp.Services;

namespace FoodDrinkApp.Tests;

public sealed class FoodItemValidatorTests
{
    [Fact]
    public void Rejects_empty_name()
    {
        Assert.False(FoodItemValidator.Validate("", 0, "d", "1", "1", "1", "1").IsValid);
    }

    [Fact]
    public void Rejects_missing_category()
    {
        Assert.False(FoodItemValidator.Validate("Tea", -1, "d", "1", "1", "1", "1").IsValid);
    }

    [Fact]
    public void Rejects_blank_description()
    {
        Assert.False(FoodItemValidator.Validate("Tea", 0, "", "1", "1", "1", "1").IsValid);
    }

    [Theory]
    [InlineData("-5")]
    [InlineData("abc")]
    [InlineData("")]
    public void Rejects_bad_calories(string calories)
    {
        Assert.False(FoodItemValidator.Validate("Tea", 0, "d", calories, "1", "1", "1").IsValid);
    }

    [Theory]
    [InlineData("6000")]
    [InlineData("99999")]
    public void Rejects_absurd_calories(string calories)
    {
        Assert.False(FoodItemValidator.Validate("Tea", 0, "Green tea", calories, "1", "1", "1").IsValid);
    }

    [Theory]
    [InlineData("1001", "1", "1")]
    [InlineData("1", "1001", "1")]
    [InlineData("1", "1", "1001")]
    public void Rejects_absurd_macro_values(string protein, string carbs, string fat)
    {
        Assert.False(FoodItemValidator.Validate("Tea", 0, "Green tea", "1", protein, carbs, fat).IsValid);
    }

    [Fact]
    public void Accepts_valid_input()
    {
        Assert.True(FoodItemValidator.Validate("Tea", 0, "Green tea", "1", "0", "2", "0").IsValid);
    }
}
