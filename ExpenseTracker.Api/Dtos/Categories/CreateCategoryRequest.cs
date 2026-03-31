using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Api.Dtos.Categories;

public class CreateCategoryRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}
