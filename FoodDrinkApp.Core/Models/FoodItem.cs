using System.Text.Json.Serialization;
using SQLite;

namespace FoodDrinkApp.Models;

/// <summary>
/// Represents a food or drink record shown by the catalogue and detail pages.
/// </summary>
public sealed class FoodItem
{
    /// <summary>
    /// Gets or sets the local SQLite primary key.
    /// </summary>
    [PrimaryKey, AutoIncrement]
    [JsonIgnore]
    public int LocalId { get; set; }

    /// <summary>
    /// Gets or sets the stable public id shared with REST-style catalogues.
    /// </summary>
    [Indexed]
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Gets or sets the food or drink display name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the meal category used by filtering.
    /// </summary>
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the short description shown on list and detail pages.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the calorie value in kilocalories.
    /// </summary>
    [JsonPropertyName("calories")]
    public int Calories { get; set; }

    /// <summary>
    /// Gets or sets protein grams.
    /// </summary>
    [JsonPropertyName("protein")]
    public int Protein { get; set; }

    /// <summary>
    /// Gets or sets carbohydrate grams.
    /// </summary>
    [JsonPropertyName("carbs")]
    public int Carbs { get; set; }

    /// <summary>
    /// Gets or sets fat grams.
    /// </summary>
    [JsonPropertyName("fat")]
    public int Fat { get; set; }

    /// <summary>
    /// Gets or sets allergy and dietary caution text.
    /// </summary>
    [JsonPropertyName("allergyNote")]
    public string AllergyNote { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets searchable free-text tags.
    /// </summary>
    [JsonPropertyName("tags")]
    public string Tags { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the record should appear in the favourites filter.
    /// </summary>
    [JsonPropertyName("isFavorite")]
    public bool IsFavorite { get; set; }

    /// <summary>
    /// Gets or sets whether the record has local user edits that should not be overwritten by catalogue sync.
    /// </summary>
    [JsonIgnore]
    public bool IsUserModified { get; set; }

    /// <summary>
    /// Gets or sets whether the record was deleted locally and should stay hidden from catalogue sync.
    /// </summary>
    [JsonIgnore]
    public bool IsDeleted { get; set; }

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
