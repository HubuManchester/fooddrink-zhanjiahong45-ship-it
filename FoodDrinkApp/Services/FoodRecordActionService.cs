using FoodDrinkApp.Models;

namespace FoodDrinkApp;

/// <summary>
/// Shared persistence actions for food records.
/// </summary>
public static class FoodRecordActionService
{
    /// <summary>
    /// Toggles and persists the favourite state for a food record.
    /// </summary>
    public static async Task<bool> ToggleFavoriteAsync(FoodItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var previousState = item.IsFavorite;
        item.IsFavorite = !item.IsFavorite;

        try
        {
            var repository = await AppDataService.GetRepositoryAsync();
            await repository.UpdateAsync(item);
            return item.IsFavorite;
        }
        catch
        {
            item.IsFavorite = previousState;
            throw;
        }
    }

    /// <summary>
    /// Deletes a food record from local storage.
    /// </summary>
    public static async Task DeleteAsync(FoodItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var repository = await AppDataService.GetRepositoryAsync();
        await repository.DeleteAsync(item);
    }
}
