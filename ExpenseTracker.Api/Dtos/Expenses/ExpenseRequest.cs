using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Api.Dtos.Expenses;

public class ExpenseRequest
{
    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "999999999")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(10)]
    public string Currency { get; set; } = "ILS";

    [Required]
    public Guid CategoryId { get; set; }

    [Required]
    public DateOnly ExpenseDate { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [MaxLength(200)]
    public string? Merchant { get; set; }

    public bool UseAiCategory { get; set; }
}
