using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Api.Dtos.Ai;

public class ClassifyExpenseRequest
{
    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;
}
