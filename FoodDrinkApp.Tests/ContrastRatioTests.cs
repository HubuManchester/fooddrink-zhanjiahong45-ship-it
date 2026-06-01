using FoodDrinkApp.Services;

namespace FoodDrinkApp.Tests;

public sealed class ContrastRatioTests
{
    [Theory]
    [InlineData("#F4EEE6", "#0D0C0F")]
    [InlineData("#BFB7C7", "#0D0C0F")]
    [InlineData("#91899E", "#0D0C0F")]
    [InlineData("#F4EEE6", "#17151B")]
    [InlineData("#BFB7C7", "#17151B")]
    [InlineData("#91899E", "#17151B")]
    [InlineData("#F4EEE6", "#221F29")]
    [InlineData("#BFB7C7", "#221F29")]
    [InlineData("#91899E", "#221F29")]
    [InlineData("#241F1A", "#FBF6EF")]
    [InlineData("#6E6258", "#FBF6EF")]
    [InlineData("#241F1A", "#FFFFFF")]
    [InlineData("#5A5048", "#FFFFFF")]
    [InlineData("#6E6258", "#FFFFFF")]
    [InlineData("#241F1A", "#F2E8DC")]
    [InlineData("#5A5048", "#F2E8DC")]
    [InlineData("#6E6258", "#F2E8DC")]
    [InlineData("#FFB8A8", "#3A201D")]
    [InlineData("#B43820", "#FFE8E0")]
    public void Body_text_pairs_meet_wcag_aa(string text, string background)
    {
        Assert.True(ContrastRatio.Between(text, background) >= 4.5);
    }

    [Theory]
    [InlineData("#FF6A3D", "#0D0C0F")]
    [InlineData("#FF6A3D", "#17151B")]
    [InlineData("#FF6A3D", "#221F29")]
    [InlineData("#FFB23D", "#0D0C0F")]
    [InlineData("#FFB23D", "#17151B")]
    [InlineData("#FFB23D", "#221F29")]
    [InlineData("#3DE0C0", "#0D0C0F")]
    [InlineData("#3DE0C0", "#17151B")]
    [InlineData("#3DE0C0", "#221F29")]
    [InlineData("#B8471F", "#FFFFFF")]
    [InlineData("#B8471F", "#FFF2E7")]
    [InlineData("#087562", "#FFFFFF")]
    [InlineData("#087562", "#F2E8DC")]
    public void Accent_text_pairs_meet_wcag_aa(string text, string background)
    {
        Assert.True(ContrastRatio.Between(text, background) >= 4.5);
    }

    [Theory]
    [InlineData("#241F1A", "#FF6A3D")]
    [InlineData("#241F1A", "#FFB23D")]
    [InlineData("#241F1A", "#3DE0C0")]
    [InlineData("#241F1A", "#78FFE2")]
    public void Button_text_pairs_meet_wcag_aa(string text, string background)
    {
        Assert.True(ContrastRatio.Between(text, background) >= 4.5);
    }

    [Fact]
    public void Invalid_hex_throws_clear_argument_exception()
    {
        var ex = Assert.Throws<ArgumentException>(() => ContrastRatio.Between("#12345", "#FFFFFF"));

        Assert.Contains("six-digit hex colour", ex.Message);
    }
}
