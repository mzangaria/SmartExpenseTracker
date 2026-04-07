namespace ExpenseTracker.Api.Exceptions;

public class InvalidCategorySelectionException(string field = "categoryId")
    : BusinessValidationException(field, "Invalid category selected.")
{
}
