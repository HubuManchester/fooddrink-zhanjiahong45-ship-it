using System.Text.Json.Serialization;
using SQLite;

namespace FoodDrinkApp.Models;

/// <summary>
/// Represents a food or drink record shown by the catalogue and detail pages.
/// </summary>
public sealed class FoodItem
{
    [PrimaryKey, AutoIncrement]
    [JsonIgnore]
    public int LocalId { get; set; }

    [Indexed]
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("calories")]
    public int Calories { get; set; }

    [JsonPropertyName("protein")]
    public int Protein { get; set; }

    [JsonPropertyName("carbs")]
    public int Carbs { get; set; }

    [JsonPropertyName("fat")]
    public int Fat { get; set; }

    [JsonPropertyName("allergyNote")]
    public string AllergyNote { get; set; } = string.Empty;

    [JsonPropertyName("tags")]
    public string Tags { get; set; } = string.Empty;

    /// <summary>
    /// Gets calories formatted for compact UI labels.
    /// </summary>
    [JsonIgnore]
    public string CaloriesLabel => $"{Calories} kcal";

    /// <summary>
    /// Gets a concise macronutrient summary.
    /// </summary>
    [JsonIgnore]
    public string MacroSummary => $"Protein {Protein}g, carbs {Carbs}g, fat {Fat}g";

    /// <summary>
    /// Gets a screen-reader-friendly nutrition summary.
    /// </summary>
    [JsonIgnore]
    public string AccessibleSummary => $"{Name}. {Category}. {Calories} kcal. {MacroSummary}. {AllergyNote}";
}
