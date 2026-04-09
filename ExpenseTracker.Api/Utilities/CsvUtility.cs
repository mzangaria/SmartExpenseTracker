using System.Globalization;
using System.Text;

namespace ExpenseTracker.Api.Utilities;

public static class CsvUtility
{
    public static string BuildRow(IEnumerable<string?> values)
    {
        return string.Join(",", values.Select(Escape));
    }

    public static async Task<List<string[]>> ReadRowsAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var content = await reader.ReadToEndAsync(cancellationToken);

        var rows = new List<string[]>();
        var currentRow = new List<string>();
        var currentValue = new StringBuilder();
        var insideQuotes = false;

        for (var index = 0; index < content.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = content[index];

            if (insideQuotes)
            {
                if (current == '"')
                {
                    if (index + 1 < content.Length && content[index + 1] == '"')
                    {
                        currentValue.Append('"');
                        index++;
                    }
                    else
                    {
                        insideQuotes = false;
                    }
                }
                else
                {
                    currentValue.Append(current);
                }

                continue;
            }

            switch (current)
            {
                case '"':
                    insideQuotes = true;
                    break;
                case ',':
                    currentRow.Add(currentValue.ToString());
                    currentValue.Clear();
                    break;
                case '\r':
                    if (index + 1 < content.Length && content[index + 1] == '\n')
                    {
                        index++;
                    }

                    currentRow.Add(currentValue.ToString());
                    currentValue.Clear();
                    rows.Add([.. currentRow]);
                    currentRow.Clear();
                    break;
                case '\n':
                    currentRow.Add(currentValue.ToString());
                    currentValue.Clear();
                    rows.Add([.. currentRow]);
                    currentRow.Clear();
                    break;
                default:
                    currentValue.Append(current);
                    break;
            }
        }

        if (insideQuotes)
        {
            throw new FormatException("CSV input has an unterminated quoted value.");
        }

        if (currentValue.Length > 0 || currentRow.Count > 0)
        {
            currentRow.Add(currentValue.ToString());
            rows.Add([.. currentRow]);
        }

        return rows
            .Where(row => row.Any(value => !string.IsNullOrWhiteSpace(value)))
            .ToList();
    }

    public static bool TryParseDecimal(string value, out decimal amount)
    {
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
    }

    public static bool TryParseDateOnly(string value, out DateOnly date)
    {
        return DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var escaped = value.Replace("\"", "\"\"", StringComparison.Ordinal);
        return value.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? $"\"{escaped}\"" : escaped;
    }
}
