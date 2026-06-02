using FoodDrinkApp.Services;

namespace FoodDrinkApp;

/// <summary>
/// Describes a catalogue import into the local SQLite repository.
/// </summary>
public sealed record CatalogImportResult(int SourceItemCount, int SyncedCount, bool UsedRemote);

/// <summary>
/// Creates and shares the app's local SQLite food repository.
/// </summary>
public static class AppDataService
{
    private const string DatabaseFileName = "nutribite.db3";
    private static readonly SemaphoreSlim InitLock = new(1, 1);
    private static FoodRepository? repository;

    /// <summary>
    /// Gets the initialized local repository, seeding it from the existing catalogue path on first run.
    /// </summary>
    public static async Task<FoodRepository> GetRepositoryAsync()
    {
        if (repository is not null)
        {
            return repository;
        }

        await InitLock.WaitAsync();
        try
        {
            if (repository is not null)
            {
                return repository;
            }

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);
            var seed = await FoodCatalogService.SearchAsync(null);
            var newRepository = new FoodRepository();
            await newRepository.InitAsync(dbPath, seed);
            repository = newRepository;
            return repository;
        }
        finally
        {
            InitLock.Release();
        }
    }

    /// <summary>
    /// Imports the current REST or local-fallback catalogue into the local SQLite database.
    /// </summary>
    public static async Task<CatalogImportResult> ImportCatalogAsync()
    {
        var localRepository = await GetRepositoryAsync();
        var importItems = await FoodCatalogService.SearchAsync(null);
        var syncedCount = await localRepository.ImportAsync(importItems);
        return new CatalogImportResult(importItems.Count, syncedCount, FoodCatalogService.LastLoadUsedRemote);
    }
}
