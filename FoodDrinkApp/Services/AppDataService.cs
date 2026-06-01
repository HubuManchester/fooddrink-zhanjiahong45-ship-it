using FoodDrinkApp.Services;

namespace FoodDrinkApp;

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
    public static async Task<int> ImportCatalogAsync()
    {
        var localRepository = await GetRepositoryAsync();
        var importItems = await FoodCatalogService.SearchAsync(null);
        return await localRepository.ImportAsync(importItems);
    }
}
