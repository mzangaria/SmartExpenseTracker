namespace ExpenseTracker.Api.Dtos.Imports;

public class CsvImportError
{
    public int RowNumber { get; set; }

    public string Message { get; set; } = string.Empty;
}
