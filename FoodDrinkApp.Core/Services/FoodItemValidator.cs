namespace FoodDrinkApp.Services;

/// <summary>
/// Represents the result of validating a food or drink form.
/// </summary>
public sealed record ValidationResult(bool IsValid, string? Message);

/// <summary>
/// Validates food and drink input without depending on MAUI controls.
/// </summary>
public static class FoodItemValidator
{
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

        foreach (var (value, field) in new[]
                 {
                     (calories, "calories"),
                     (protein, "protein"),
                     (carbs, "carbs"),
                     (fat, "fat")
                 })
        {
            if (!int.TryParse(value, out var number) || number < 0)
            {
                return new(false, $"Please enter a valid non-negative number for {field}.");
            }
        }

        return new(true, null);
    }
}
