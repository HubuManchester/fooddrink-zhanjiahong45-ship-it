using FoodDrinkApp.Models;
using SQLite;

namespace FoodDrinkApp.Services;

/// <summary>
/// Provides persistent local SQLite CRUD operations for food and drink records.
/// </summary>
public sealed class FoodRepository
{
    private SQLiteAsyncConnection? database;

    /// <summary>
    /// Opens the SQLite database, creates the food table, and seeds it when empty.
    /// </summary>
    public async Task InitAsync(string dbPath, IEnumerable<FoodItem>? seed)
    {
        if (database is not null)
        {
            return;
        }

        SQLitePCL.Batteries_V2.Init();
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        database = new SQLiteAsyncConnection(dbPath);
        await database.CreateTableAsync<FoodItem>();

        if (seed is not null && await database.Table<FoodItem>().CountAsync() == 0)
        {
            await ImportAsync(seed);
        }
    }

    /// <summary>
    /// Gets all locally stored records ordered by display name.
    /// </summary>
    public Task<List<FoodItem>> GetAllAsync() =>
        Database.Table<FoodItem>().OrderBy(item => item.Name).ToListAsync();

    /// <summary>
    /// Searches locally stored records by name, category, description, or tags.
    /// </summary>
    public async Task<IReadOnlyList<FoodItem>> SearchAsync(string? query)
    {
        var items = await GetAllAsync();
        if (string.IsNullOrWhiteSpace(query))
        {
            return items;
        }

        var normalised = query.Trim();
        return items
            .Where(item =>
                item.Name.Contains(normalised, StringComparison.OrdinalIgnoreCase) ||
                item.Category.Contains(normalised, StringComparison.OrdinalIgnoreCase) ||
                item.Description.Contains(normalised, StringComparison.OrdinalIgnoreCase) ||
                item.Tags.Contains(normalised, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// Gets a locally stored record by its REST-compatible public id.
    /// </summary>
    public async Task<FoodItem?> GetByIdAsync(string id) =>
        await Database.Table<FoodItem>().Where(item => item.Id == id).FirstOrDefaultAsync();

    /// <summary>
    /// Gets a locally stored record by its SQLite primary key.
    /// </summary>
    public async Task<FoodItem?> GetByLocalIdAsync(int localId) =>
        await Database.Table<FoodItem>().Where(item => item.LocalId == localId).FirstOrDefaultAsync();

    /// <summary>
    /// Adds a new local record.
    /// </summary>
    public Task<int> AddAsync(FoodItem item)
    {
        EnsurePublicId(item);
        return Database.InsertAsync(item);
    }

    /// <summary>
    /// Updates an existing local record.
    /// </summary>
    public Task<int> UpdateAsync(FoodItem item)
    {
        EnsurePublicId(item);
        return Database.UpdateAsync(item);
    }

    /// <summary>
    /// Deletes an existing local record.
    /// </summary>
    public Task<int> DeleteAsync(FoodItem item) =>
        Database.DeleteAsync(item);

    /// <summary>
    /// Deletes a local record by public id.
    /// </summary>
    public async Task<bool> DeleteByIdAsync(string id)
    {
        var item = await GetByIdAsync(id);
        if (item is null)
        {
            return false;
        }

        await DeleteAsync(item);
        return true;
    }

    /// <summary>
    /// Imports REST or fallback records into the local database without duplicating public ids.
    /// </summary>
    public async Task<int> ImportAsync(IEnumerable<FoodItem> items)
    {
        var changed = 0;
        foreach (var item in items)
        {
            var incoming = CloneForStorage(item);
            EnsurePublicId(incoming);

            var existing = await GetByIdAsync(incoming.Id);
            if (existing is null)
            {
                await Database.InsertAsync(incoming);
                changed++;
                continue;
            }

            incoming.LocalId = existing.LocalId;
            await Database.UpdateAsync(incoming);
            changed++;
        }

        return changed;
    }

    /// <summary>
    /// Closes the SQLite connection, mainly for deterministic cleanup in tests.
    /// </summary>
    public async Task CloseAsync()
    {
        if (database is null)
        {
            return;
        }

        await database.CloseAsync();
        database = null;
    }

    private SQLiteAsyncConnection Database =>
        database ?? throw new InvalidOperationException("FoodRepository.InitAsync must be called before use.");

    private static void EnsurePublicId(FoodItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Id))
        {
            item.Id = Guid.NewGuid().ToString("N");
        }
    }

    private static FoodItem CloneForStorage(FoodItem item) =>
        new()
        {
            Id = item.Id,
            Name = item.Name,
            Category = item.Category,
            Description = item.Description,
            Calories = item.Calories,
            Protein = item.Protein,
            Carbs = item.Carbs,
            Fat = item.Fat,
            AllergyNote = item.AllergyNote,
            Tags = item.Tags
        };
}
