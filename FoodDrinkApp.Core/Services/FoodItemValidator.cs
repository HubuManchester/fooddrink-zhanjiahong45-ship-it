namespace FoodDrinkApp.Services;

/// <summary>
/// Represents the result of validating a food or drink form.
/// </summary>
/// <param name="IsValid">Whether validation passed.</param>
/// <param name="Message">The user-facing validation message when validation failed.</param>
public sealed record ValidationResult(bool IsValid, string? Message);

/// <summary>
/// Validates food and drink input without depending on MAUI controls.
/// </summary>
public static class FoodItemValidator
{
    private const int MaxCalories = 5000;
    private const int MaxMacroGrams = 1000;

    /// <summary>
    /// Validates user-entered form text before the MAUI page creates a food item.
    /// </summary>
    public static ValidationResult Validate(
        string? name,
        int categoryIndex,
        string? description,
        string? calories,
        string? protein,
        string? carbs,
        string? fat)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return new(false, "Please enter a food or drink name.");
        }

        if (categoryIndex < 0)
        {
            return new(false, "Please choose a category.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            return new(false, "Please add a short description.");
        }

        foreach (var (value, field, max, unit) in new[]
                 {
                     (calories, "calories", MaxCalories, "kcal"),
                     (protein, "protein", MaxMacroGrams, "g"),
                     (carbs, "carbs", MaxMacroGrams, "g"),
                     (fat, "fat", MaxMacroGrams, "g")
                 })
        {
            if (!int.TryParse(value, out var number) || number < 0)
            {
                return new(false, $"Please enter a valid non-negative number for {field}.");
            }

            if (number > max)
            {
                return new(false, $"Please enter a realistic value for {field} ({max} {unit} or less).");
            }
        }

        return new(true, null);
    }
}
