namespace SIMS.Infrastructure;

internal static class CsvFile
{
    public static void EnsureExists(string filePath, string header)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        if (!File.Exists(filePath)) File.WriteAllLines(filePath, new[] { header });
    }
}
