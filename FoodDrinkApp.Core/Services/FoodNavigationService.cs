namespace FoodDrinkApp.Services;

/// <summary>
/// Resolves neighbouring food records in an ordered list.
/// </summary>
public static class FoodNavigationService
{
    /// <summary>
    /// Gets the adjacent public id for the current record.
    /// </summary>
    public static string? GetAdjacentId(
        IReadOnlyList<string> orderedIds,
        string currentId,
        int offset,
        bool wrap = true)
    {
        ArgumentNullException.ThrowIfNull(orderedIds);

        if (string.IsNullOrWhiteSpace(currentId) || offset == 0)
        {
            return null;
        }

        var ids = orderedIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToArray();

        if (ids.Length <= 1)
        {
            return null;
        }

        var currentIndex = Array.FindIndex(ids, id => string.Equals(id, currentId, StringComparison.Ordinal));
        if (currentIndex < 0)
        {
            return null;
        }

        var nextIndex = currentIndex + offset;
        if (wrap)
        {
            nextIndex = ((nextIndex % ids.Length) + ids.Length) % ids.Length;
        }
        else if (nextIndex < 0 || nextIndex >= ids.Length)
        {
            return null;
        }

        return ids[nextIndex];
    }
}
