namespace ExpenseTracker.Api.Dtos.Imports;

public class CsvImportResult
{
    public int ImportedCount { get; set; }

    public int FailedCount => Errors.Count;

    public List<CsvImportError> Errors { get; set; } = [];
}
