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
        await EnsureFoodItemSchemaAsync();

        if (seed is not null && await database.Table<FoodItem>().CountAsync() == 0)
        {
            await ImportAsync(seed);
        }
    }

    /// <summary>
    /// Gets all locally stored records ordered by display name.
    /// </summary>
    public Task<List<FoodItem>> GetAllAsync() =>
        Database.Table<FoodItem>()
            .Where(item => item.IsDeleted == false)
            .OrderBy(item => item.Name)
            .ToListAsync();

    /// <summary>
    /// Gets the number of locally stored records.
    /// </summary>
    public Task<int> CountAsync() =>
        Database.Table<FoodItem>()
            .Where(item => item.IsDeleted == false)
            .CountAsync();

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
        await Database.Table<FoodItem>()
            .Where(item => item.Id == id && item.IsDeleted == false)
            .FirstOrDefaultAsync();

    /// <summary>
    /// Gets a locally stored record by its SQLite primary key.
    /// </summary>
    public async Task<FoodItem?> GetByLocalIdAsync(int localId) =>
        await Database.Table<FoodItem>()
            .Where(item => item.LocalId == localId && item.IsDeleted == false)
            .FirstOrDefaultAsync();

    /// <summary>
    /// Adds a new local record.
    /// </summary>
    public Task<int> AddAsync(FoodItem item)
    {
        EnsurePublicId(item);
        item.IsUserModified = true;
        item.IsDeleted = false;
        return Database.InsertAsync(item);
    }

    /// <summary>
    /// Updates an existing local record.
    /// </summary>
    public Task<int> UpdateAsync(FoodItem item)
    {
        EnsurePublicId(item);
        item.IsUserModified = true;
        item.IsDeleted = false;
        return Database.UpdateAsync(item);
    }

    /// <summary>
    /// Updates only the favourite state without marking the full record as user-edited.
    /// </summary>
    public Task<int> UpdateFavoriteAsync(FoodItem item)
    {
        EnsurePublicId(item);
        item.IsDeleted = false;
        return Database.UpdateAsync(item);
    }

    /// <summary>
    /// Deletes an existing local record.
    /// </summary>
    public async Task<int> DeleteAsync(FoodItem item)
    {
        EnsurePublicId(item);

        var existing = item.LocalId > 0
            ? await GetByLocalIdIncludingDeletedAsync(item.LocalId)
            : await GetByIdIncludingDeletedAsync(item.Id);

        if (existing is null)
        {
            return 0;
        }

        existing.IsDeleted = true;
        existing.IsUserModified = true;
        item.LocalId = existing.LocalId;
        item.IsDeleted = true;
        item.IsUserModified = true;
        return await Database.UpdateAsync(existing);
    }

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

            var existing = await GetByIdIncludingDeletedAsync(incoming.Id);
            if (existing is null)
            {
                await Database.InsertAsync(incoming);
                changed++;
                continue;
            }

            if (existing.IsDeleted || existing.IsUserModified)
            {
                continue;
            }

            incoming.LocalId = existing.LocalId;
            incoming.IsFavorite = existing.IsFavorite;
            incoming.IsUserModified = existing.IsUserModified;
            incoming.IsDeleted = existing.IsDeleted;
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

    private async Task EnsureFoodItemSchemaAsync()
    {
        await AddColumnIfMissingAsync(nameof(FoodItem.IsUserModified), "INTEGER NOT NULL DEFAULT 0");
        await AddColumnIfMissingAsync(nameof(FoodItem.IsDeleted), "INTEGER NOT NULL DEFAULT 0");
    }

    private async Task AddColumnIfMissingAsync(string columnName, string definition)
    {
        var columns = await Database.QueryAsync<TableColumnInfo>($"PRAGMA table_info({nameof(FoodItem)})");
        if (columns.Any(column => string.Equals(column.Name, columnName, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        await Database.ExecuteAsync($"ALTER TABLE {nameof(FoodItem)} ADD COLUMN {columnName} {definition}");
    }

    private async Task<FoodItem?> GetByIdIncludingDeletedAsync(string id) =>
        await Database.Table<FoodItem>().Where(item => item.Id == id).FirstOrDefaultAsync();

    private async Task<FoodItem?> GetByLocalIdIncludingDeletedAsync(int localId) =>
        await Database.Table<FoodItem>().Where(item => item.LocalId == localId).FirstOrDefaultAsync();

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
            Tags = item.Tags,
            IsFavorite = item.IsFavorite,
            IsUserModified = false,
            IsDeleted = false
        };

    private sealed class TableColumnInfo
    {
        [Column("name")]
        public string Name { get; set; } = string.Empty;
    }
}
