using FoodDrinkApp.Services;

namespace FoodDrinkApp.Tests;

public sealed class ContrastRatioTests
{
    [Theory]
    [InlineData("#3A2518", "#FFFDF8")]
    [InlineData("#6E5A47", "#FFFFFA")]
    [InlineData("#3A2518", "#FFF4E3")]
    [InlineData("#FFF2DF", "#1A1712")]
    [InlineData("#D8C4AC", "#1A1712")]
    public void Body_text_pairs_meet_wcag_aa(string text, string background)
    {
        Assert.True(ContrastRatio.Between(text, background) >= 4.5);
    }

    [Theory]
    [InlineData("#D9472B", "#FFFFFA")]
    [InlineData("#2F7A4F", "#FFFFFA")]
    [InlineData("#FFB15D", "#1A1712")]
    [InlineData("#8CE6A1", "#1A1712")]
    public void Accent_and_large_text_pairs_meet_wcag_aa_large_text(string text, string background)
    {
        Assert.True(ContrastRatio.Between(text, background) >= 3.0);
    }

    [Fact]
    public void Invalid_hex_throws_clear_argument_exception()
    {
        var ex = Assert.Throws<ArgumentException>(() => ContrastRatio.Between("#12345", "#FFFFFF"));

        Assert.Contains("six-digit hex colour", ex.Message);
    }
}
