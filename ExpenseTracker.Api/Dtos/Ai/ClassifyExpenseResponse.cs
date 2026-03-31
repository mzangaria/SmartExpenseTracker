namespace ExpenseTracker.Api.Dtos.Ai;

public class ClassifyExpenseResponse
{
    public bool Success { get; set; }

    public string? SuggestedCategory { get; set; }

    public Guid? SuggestedCategoryId { get; set; }

    public string? Message { get; set; }
}
