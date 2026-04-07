namespace ExpenseTracker.Api.Exceptions;

public class BusinessValidationException(string field, string message) : Exception(message)
{
    public string Field { get; } = field;
}
