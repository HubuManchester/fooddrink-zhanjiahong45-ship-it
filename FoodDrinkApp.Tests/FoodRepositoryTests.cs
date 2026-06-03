using FoodDrinkApp.Models;
using FoodDrinkApp.Services;

namespace FoodDrinkApp.Tests;

public sealed class FoodRepositoryTests
{
    [Fact]
    public async Task Crud_round_trip_adds_reads_updates_and_deletes_record()
    {
        var dbPath = CreateTempDatabasePath();
        var repository = new FoodRepository();
        try
        {
            await repository.InitAsync(dbPath, []);

            var item = SampleItem("smoothie-1", "Berry Smoothie", "Drink", "berries breakfast");
            item.IsFavorite = true;
            await repository.AddAsync(item);

            var created = await repository.GetByIdAsync("smoothie-1");
            Assert.NotNull(created);
            Assert.True(created!.LocalId > 0);
            Assert.True(created.IsFavorite);

            created.Description = "Updated with oats and yogurt.";
            created.Calories = 310;
            created.IsFavorite = false;
            await repository.UpdateAsync(created);

            var updated = await repository.GetByLocalIdAsync(created.LocalId);
            Assert.Equal("Updated with oats and yogurt.", updated!.Description);
            Assert.Equal(310, updated.Calories);
            Assert.False(updated.IsFavorite);

            await repository.DeleteAsync(updated);

            Assert.Null(await repository.GetByIdAsync("smoothie-1"));
        }
        finally
        {
            await repository.CloseAsync();
            DeleteTempDatabase(dbPath);
        }
    }

    [Fact]
    public async Task Init_seeds_empty_database_and_search_matches_seed_fields()
    {
        var dbPath = CreateTempDatabasePath();
        var repository = new FoodRepository();
        try
        {
            await repository.InitAsync(
                dbPath,
                [
                    SampleItem("lunch-1", "Chicken Rice Box", "Lunch", "protein meal prep"),
                    SampleItem("drink-1", "Iced Matcha", "Drink", "matcha caffeine")
                ]);

            var allItems = await repository.GetAllAsync();
            Assert.Equal(["Chicken Rice Box", "Iced Matcha"], allItems.Select(item => item.Name));

            var tagMatches = await repository.SearchAsync("protein");
            Assert.Single(tagMatches);
            Assert.Equal("Chicken Rice Box", tagMatches[0].Name);

            var categoryMatches = await repository.SearchAsync("  drink  ");
            Assert.Single(categoryMatches);
            Assert.Equal("Iced Matcha", categoryMatches[0].Name);
        }
        finally
        {
            await repository.CloseAsync();
            DeleteTempDatabase(dbPath);
        }
    }

    [Fact]
    public async Task UpdateAsync_persists_favorite_toggle_when_reloaded()
    {
        var dbPath = CreateTempDatabasePath();
        var repository = new FoodRepository();
        try
        {
            await repository.InitAsync(dbPath, []);

            await repository.AddAsync(SampleItem("toast-1", "Avocado Toast", "Breakfast", "toast brunch"));

            var item = await repository.GetByIdAsync("toast-1");
            Assert.NotNull(item);
            Assert.False(item!.IsFavorite);

            item.IsFavorite = true;
            await repository.UpdateAsync(item);

            var reloaded = await repository.GetByIdAsync("toast-1");
            Assert.True(reloaded!.IsFavorite);
        }
        finally
        {
            await repository.CloseAsync();
            DeleteTempDatabase(dbPath);
        }
    }

    [Fact]
    public async Task DeleteByIdAsync_removes_record()
    {
        var dbPath = CreateTempDatabasePath();
        var repository = new FoodRepository();
        try
        {
            await repository.InitAsync(dbPath, []);
            await repository.AddAsync(SampleItem("salad-1", "Lentil Salad", "Lunch", "lentils greens"));

            var deleted = await repository.DeleteByIdAsync("salad-1");

            Assert.True(deleted);
            Assert.Null(await repository.GetByIdAsync("salad-1"));
            Assert.False(await repository.DeleteByIdAsync("salad-1"));
        }
        finally
        {
            await repository.CloseAsync();
            DeleteTempDatabase(dbPath);
        }
    }

    [Fact]
    public async Task Import_updates_existing_public_ids_without_duplicates()
    {
        var dbPath = CreateTempDatabasePath();
        var repository = new FoodRepository();
        try
        {
            var existingItem = SampleItem("meal-1", "Old Name", "Lunch", "protein");
            existingItem.IsFavorite = true;
            await repository.InitAsync(dbPath, [existingItem]);

            var updatedItem = SampleItem("meal-1", "Updated Name", "Dinner", "updated");

            await repository.ImportAsync([updatedItem]);

            var allItems = await repository.GetAllAsync();
            Assert.Single(allItems);
            Assert.Equal("Updated Name", allItems[0].Name);
            Assert.Equal("Dinner", allItems[0].Category);
            Assert.True(allItems[0].IsFavorite);
        }
        finally
        {
            await repository.CloseAsync();
            DeleteTempDatabase(dbPath);
        }
    }

    [Fact]
    public async Task Import_preserves_user_modified_catalogue_records()
    {
        var dbPath = CreateTempDatabasePath();
        var repository = new FoodRepository();
        try
        {
            await repository.InitAsync(dbPath, [SampleItem("remote-1", "Original Remote Item", "Lunch", "original")]);

            var localEdit = await repository.GetByIdAsync("remote-1");
            Assert.NotNull(localEdit);
            localEdit!.Name = "My Edited Item";
            localEdit.Category = "Custom";
            localEdit.Calories = 777;
            await repository.UpdateAsync(localEdit);

            await repository.ImportAsync([SampleItem("remote-1", "Server Item", "Dinner", "server")]);

            var reloaded = await repository.GetByIdAsync("remote-1");
            Assert.NotNull(reloaded);
            Assert.Equal("My Edited Item", reloaded!.Name);
            Assert.Equal("Custom", reloaded.Category);
            Assert.Equal(777, reloaded.Calories);
            Assert.True(reloaded.IsUserModified);
        }
        finally
        {
            await repository.CloseAsync();
            DeleteTempDatabase(dbPath);
        }
    }

    [Fact]
    public async Task Import_keeps_locally_deleted_catalogue_records_hidden()
    {
        var dbPath = CreateTempDatabasePath();
        var repository = new FoodRepository();
        try
        {
            await repository.InitAsync(dbPath, [SampleItem("remote-1", "Remote Item", "Lunch", "remote")]);

            Assert.True(await repository.DeleteByIdAsync("remote-1"));
            Assert.Null(await repository.GetByIdAsync("remote-1"));
            Assert.Equal(0, await repository.CountAsync());

            await repository.ImportAsync([SampleItem("remote-1", "Remote Item Restored", "Dinner", "remote")]);

            Assert.Null(await repository.GetByIdAsync("remote-1"));
            Assert.Empty(await repository.GetAllAsync());
        }
        finally
        {
            await repository.CloseAsync();
            DeleteTempDatabase(dbPath);
        }
    }

    [Fact]
    public async Task UpdateFavoriteAsync_preserves_favourite_without_blocking_catalogue_updates()
    {
        var dbPath = CreateTempDatabasePath();
        var repository = new FoodRepository();
        try
        {
            await repository.InitAsync(dbPath, [SampleItem("remote-1", "Original Remote Item", "Lunch", "original")]);

            var item = await repository.GetByIdAsync("remote-1");
            Assert.NotNull(item);
            item!.IsFavorite = true;
            await repository.UpdateFavoriteAsync(item);

            await repository.ImportAsync([SampleItem("remote-1", "Updated Remote Item", "Dinner", "updated")]);

            var reloaded = await repository.GetByIdAsync("remote-1");
            Assert.NotNull(reloaded);
            Assert.Equal("Updated Remote Item", reloaded!.Name);
            Assert.Equal("Dinner", reloaded.Category);
            Assert.True(reloaded.IsFavorite);
            Assert.False(reloaded.IsUserModified);
        }
        finally
        {
            await repository.CloseAsync();
            DeleteTempDatabase(dbPath);
        }
    }

    [Fact]
    public async Task Import_merges_remote_catalogue_preserving_user_items_and_favorites()
    {
        var dbPath = CreateTempDatabasePath();
        var repository = new FoodRepository();
        try
        {
            await repository.InitAsync(dbPath, []);

            var existingRemoteItem = SampleItem("remote-1", "Old Remote Item", "Lunch", "old");
            existingRemoteItem.IsFavorite = true;
            await repository.ImportAsync([existingRemoteItem]);
            await repository.AddAsync(SampleItem("user-added-1", "User Smoothie", "Drink", "custom local"));

            var updatedRemoteItem = SampleItem("remote-1", "Updated Remote Item", "Dinner", "updated remote");
            updatedRemoteItem.IsFavorite = false;
            var newRemoteItem = SampleItem("remote-2", "New Remote Item", "Snack", "new remote");
            newRemoteItem.IsFavorite = true;

            await repository.ImportAsync([updatedRemoteItem, newRemoteItem]);

            var allItems = await repository.GetAllAsync();
            Assert.Equal(3, allItems.Count);

            var updated = Assert.Single(allItems, item => item.Id == "remote-1");
            Assert.Equal("Updated Remote Item", updated.Name);
            Assert.Equal("Dinner", updated.Category);
            Assert.True(updated.IsFavorite);

            Assert.Contains(allItems, item => item.Id == "user-added-1" && item.Name == "User Smoothie");

            var inserted = Assert.Single(allItems, item => item.Id == "remote-2");
            Assert.True(inserted.IsFavorite);
        }
        finally
        {
            await repository.CloseAsync();
            DeleteTempDatabase(dbPath);
        }
    }

    private static FoodItem SampleItem(string id, string name, string category, string tags) =>
        new()
        {
            Id = id,
            Name = name,
            Category = category,
            Description = $"{name} description",
            Calories = 250,
            Protein = 12,
            Carbs = 30,
            Fat = 8,
            AllergyNote = "No common allergens recorded.",
            Tags = tags
        };

    private static string CreateTempDatabasePath() =>
        Path.Combine(Path.GetTempPath(), $"nutribite-tests-{Guid.NewGuid():N}.db3");

    private static void DeleteTempDatabase(string dbPath)
    {
        foreach (var path in new[] { dbPath, $"{dbPath}-wal", $"{dbPath}-shm" })
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
