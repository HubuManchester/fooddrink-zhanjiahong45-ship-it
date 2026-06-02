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
            await repository.InitAsync(dbPath, [SampleItem("meal-1", "Old Name", "Lunch", "protein")]);

            var updatedItem = SampleItem("meal-1", "Updated Name", "Dinner", "updated");
            updatedItem.IsFavorite = true;

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
