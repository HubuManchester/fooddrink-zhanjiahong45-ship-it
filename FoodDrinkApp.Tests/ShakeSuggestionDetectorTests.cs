using System.Numerics;
using FoodDrinkApp.Services;

namespace FoodDrinkApp.Tests;

public sealed class ShakeSuggestionDetectorTests
{
    [Fact]
    public void ShouldSuggest_ignores_initial_stationary_gravity_reading()
    {
        var detector = new ShakeSuggestionDetector();

        var triggered = detector.ShouldSuggest(new Vector3(0, 0, 1));

        Assert.False(triggered);
    }

    [Fact]
    public void ShouldSuggest_triggers_on_large_acceleration_change()
    {
        var detector = new ShakeSuggestionDetector();

        detector.ShouldSuggest(new Vector3(0, 0, 1));
        var triggered = detector.ShouldSuggest(new Vector3(0.9f, 0, 0.4f));

        Assert.True(triggered);
    }

    [Fact]
    public void ShouldSuggest_triggers_on_high_acceleration_magnitude()
    {
        var detector = new ShakeSuggestionDetector();

        var triggered = detector.ShouldSuggest(new Vector3(1.7f, 0, 0));

        Assert.True(triggered);
    }

    [Fact]
    public void ShouldSuggest_ignores_small_sensor_noise()
    {
        var detector = new ShakeSuggestionDetector();

        detector.ShouldSuggest(new Vector3(0, 0, 1));
        var triggered = detector.ShouldSuggest(new Vector3(0.05f, -0.04f, 0.98f));

        Assert.False(triggered);
    }

    [Fact]
    public void Reset_discards_previous_acceleration_sample()
    {
        var detector = new ShakeSuggestionDetector();

        detector.ShouldSuggest(new Vector3(0, 0, 1));
        detector.Reset();
        var triggered = detector.ShouldSuggest(new Vector3(0.9f, 0, 0.4f));

        Assert.False(triggered);
    }
}
