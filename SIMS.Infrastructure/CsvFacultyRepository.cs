using SIMS.Domain;

namespace SIMS.Infrastructure;

public class CsvFacultyRepository : IFacultyRepository
{
    private const string Header = "Id,Username,Email,PasswordHash,EmployeeId,Department";
    private readonly string _filePath;

    public CsvFacultyRepository(string filePath)
    {
        _filePath = filePath;
        CsvFile.EnsureExists(_filePath, Header);
    }

    public IEnumerable<Faculty> GetAll() =>
        File.ReadLines(_filePath)
            .Skip(1)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(TryParse)
            .Where(f => f is not null)!;

    public Faculty? GetById(int id) => GetAll().FirstOrDefault(f => f.Id == id);

    public Faculty? GetByUsername(string username) =>
        GetAll().FirstOrDefault(f => string.Equals(f.Username, username, StringComparison.OrdinalIgnoreCase));

    public void Add(Faculty faculty)
    {
        var all = GetAll().ToList();
        faculty.Id = all.Count == 0 ? 1 : all.Max(f => f.Id) + 1;
        all.Add(faculty);
        WriteAll(all);
    }

    public void Update(Faculty faculty)
    {
        var all = GetAll().ToList();
        var index = all.FindIndex(f => f.Id == faculty.Id);
        if (index == -1) throw new InvalidOperationException($"Faculty {faculty.Id} not found.");
        all[index] = faculty;
        WriteAll(all);
    }

    public void Delete(int id)
    {
        var all = GetAll().Where(f => f.Id != id).ToList();
        WriteAll(all);
    }

    private void WriteAll(IEnumerable<Faculty> all)
    {
        var lines = new List<string> { Header };
        lines.AddRange(all.Select(ToCsvLine));
        File.WriteAllLines(_filePath, lines);
    }

    private static Faculty? TryParse(string line)
    {
        var f = CsvUtils.SplitLine(line);
        if (f.Length < 6) return null;
        if (!int.TryParse(f[0], out var id)) return null;

        return new Faculty
        {
            Id = id,
            Username = f[1],
            Email = f[2],
            PasswordHash = f[3],
            EmployeeId = f[4],
            Department = f[5]
        };
    }

    private static string ToCsvLine(Faculty f) => CsvUtils.JoinFields(new[]
    {
        f.Id.ToString(), f.Username, f.Email, f.PasswordHash, f.EmployeeId, f.Department
    });
}
