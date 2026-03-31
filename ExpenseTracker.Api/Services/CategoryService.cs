using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Dtos.Categories;
using ExpenseTracker.Api.Entities;
using ExpenseTracker.Api.Enums;
using ExpenseTracker.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Services;

public class CategoryService(AppDbContext dbContext) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryResponse>> GetAvailableCategoriesAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Categories
            .AsNoTracking()
            .Where(category => category.Type == CategoryType.System || category.UserId == userId)
            .OrderBy(category => category.Type)
            .ThenBy(category => category.Name)
            .Select(category => new CategoryResponse
            {
                Id = category.Id,
                Name = category.Name,
                Type = category.Type.ToString().ToLowerInvariant()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoryResponse?> CreateCustomCategoryAsync(Guid userId, string name, CancellationToken cancellationToken)
    {
        var normalizedName = name.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return null;
        }

        var loweredName = normalizedName.ToLowerInvariant();
        var exists = await dbContext.Categories.AnyAsync(
            category => (category.Type == CategoryType.System || category.UserId == userId) &&
                        category.Name.ToLower() == loweredName,
            cancellationToken);

        if (exists)
        {
            return null;
        }

        var category = new Category
        {
            Name = normalizedName,
            Type = CategoryType.Custom,
            UserId = userId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Type = category.Type.ToString().ToLowerInvariant()
        };
    }

    public Task<Category?> GetOwnedCategoryAsync(Guid userId, Guid categoryId, CancellationToken cancellationToken)
    {
        return dbContext.Categories.FirstOrDefaultAsync(
            category => category.Id == categoryId &&
                        (category.Type == CategoryType.System || category.UserId == userId),
            cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> GetAllowedCategoryEntitiesAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Categories
            .Where(category => category.Type == CategoryType.System || category.UserId == userId)
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken);
    }
}
