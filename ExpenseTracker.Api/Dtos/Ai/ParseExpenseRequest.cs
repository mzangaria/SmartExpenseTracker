using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Api.Dtos.Ai;

public class ParseExpenseRequest
{
    [Required]
    [MaxLength(500)]
    public string Text { get; set; } = string.Empty;
}
