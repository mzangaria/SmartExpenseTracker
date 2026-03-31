using ExpenseTracker.Api.Dtos.Categories;
using ExpenseTracker.Api.Entities;

namespace ExpenseTracker.Api.Services.Interfaces;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryResponse>> GetAvailableCategoriesAsync(Guid userId, CancellationToken cancellationToken);

    Task<CategoryResponse?> CreateCustomCategoryAsync(Guid userId, string name, CancellationToken cancellationToken);

    Task<Category?> GetOwnedCategoryAsync(Guid userId, Guid categoryId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Category>> GetAllowedCategoryEntitiesAsync(Guid userId, CancellationToken cancellationToken);
}
