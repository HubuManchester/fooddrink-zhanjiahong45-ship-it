using System.Numerics;

namespace FoodDrinkApp.Services;

/// <summary>
/// Detects shake-like movement from accelerometer readings.
/// </summary>
public sealed class ShakeSuggestionDetector
{
    private const float AccelerationMagnitudeThreshold = 1.55f;
    private const float AccelerationChangeThreshold = 0.55f;
    private Vector3? previousAcceleration;

    /// <summary>
    /// Returns true when the latest acceleration looks like a deliberate shake or strong tilt.
    /// </summary>
    public bool ShouldSuggest(Vector3 acceleration)
    {
        var previous = previousAcceleration;
        previousAcceleration = acceleration;

        if (acceleration.Length() >= AccelerationMagnitudeThreshold)
        {
            return true;
        }

        return previous is not null &&
            Vector3.Distance(previous.Value, acceleration) >= AccelerationChangeThreshold;
    }

    /// <summary>
    /// Clears the previous sample when shake suggestions are restarted.
    /// </summary>
    public void Reset() => previousAcceleration = null;
}
