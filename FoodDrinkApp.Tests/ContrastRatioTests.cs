using FoodDrinkApp.Services;

namespace FoodDrinkApp.Tests;

public sealed class ContrastRatioTests
{
    [Theory]
    [InlineData("#F4EEE6", "#0D0C0F")]
    [InlineData("#BFB7C7", "#0D0C0F")]
    [InlineData("#F4EEE6", "#17151B")]
    [InlineData("#BFB7C7", "#17151B")]
    [InlineData("#241F1A", "#FBF6EF")]
    [InlineData("#5A5048", "#FFFFFF")]
    [InlineData("#5A5048", "#FBF6EF")]
    public void Body_text_pairs_meet_wcag_aa(string text, string background)
    {
        Assert.True(ContrastRatio.Between(text, background) >= 4.5);
    }

    [Theory]
    [InlineData("#FF6A3D", "#0D0C0F")]
    [InlineData("#FFB23D", "#0D0C0F")]
    [InlineData("#3DE0C0", "#0D0C0F")]
    [InlineData("#D24E22", "#FFFFFF")]
    [InlineData("#0F8E76", "#FFFFFF")]
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
