using ExpenseTracker.Api.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Data;

public static class AppDbSeeder
{
    public static async Task SeedAsync(AppDbContext dbContext)
    {
        var existingSystemCategories = await dbContext.Categories
            .Where(category => category.Type == CategoryType.System)
            .Select(category => category.Name)
            .ToListAsync();

        var missingCategories = SystemCategoryCatalog.Names
            .Except(existingSystemCategories, StringComparer.OrdinalIgnoreCase)
            .Select(name => new Entities.Category
            {
                Name = name,
                Type = CategoryType.System,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            })
            .ToList();

        if (missingCategories.Count == 0)
        {
            return;
        }

        dbContext.Categories.AddRange(missingCategories);
        await dbContext.SaveChangesAsync();
    }
}
