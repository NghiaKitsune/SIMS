using System.Text;

namespace SIMS.Infrastructure;

// Minimal CSV field splitting/escaping shared by all Csv*Repository classes.
// Handles quoted fields so a value such as "Design, Patterns" round-trips
// correctly instead of being torn into two columns.
internal static class CsvUtils
{
    public static string[] SplitLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == ',')
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString());
        return fields.ToArray();
    }

    public static string EscapeField(string? field)
    {
        field ??= string.Empty;
        return field.Contains(',') || field.Contains('"') || field.Contains('\n')
            ? "\"" + field.Replace("\"", "\"\"") + "\""
            : field;
    }

    public static string JoinFields(IEnumerable<string?> fields) =>
        string.Join(",", fields.Select(EscapeField));
}
