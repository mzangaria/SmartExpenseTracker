namespace ExpenseTracker.Api.Dtos.Expenses;

public class ExpenseQueryParameters
{
    public int? Month { get; set; }

    public int? Year { get; set; }

    public Guid? CategoryId { get; set; }

    public decimal? MinAmount { get; set; }

    public decimal? MaxAmount { get; set; }

    public string? Search { get; set; }
}
