using SIMS.Domain;

namespace SIMS.Infrastructure;

public class CsvAdministratorRepository : IAdministratorRepository
{
    private const string Header = "Id,Username,Email,PasswordHash,AdminLevel";
    private readonly string _filePath;

    public CsvAdministratorRepository(string filePath)
    {
        _filePath = filePath;
        CsvFile.EnsureExists(_filePath, Header);
    }

    public IEnumerable<Administrator> GetAll() =>
        File.ReadLines(_filePath)
            .Skip(1)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(TryParse)
            .Where(a => a is not null)!;

    public Administrator? GetById(int id) => GetAll().FirstOrDefault(a => a.Id == id);

    public Administrator? GetByUsername(string username) =>
        GetAll().FirstOrDefault(a => string.Equals(a.Username, username, StringComparison.OrdinalIgnoreCase));

    public void Add(Administrator administrator)
    {
        var all = GetAll().ToList();
        administrator.Id = all.Count == 0 ? 1 : all.Max(a => a.Id) + 1;
        all.Add(administrator);
        WriteAll(all);
    }

    public void Update(Administrator administrator)
    {
        var all = GetAll().ToList();
        var index = all.FindIndex(a => a.Id == administrator.Id);
        if (index == -1) throw new InvalidOperationException($"Administrator {administrator.Id} not found.");
        all[index] = administrator;
        WriteAll(all);
    }

    public void Delete(int id)
    {
        var all = GetAll().Where(a => a.Id != id).ToList();
        WriteAll(all);
    }

    private void WriteAll(IEnumerable<Administrator> all)
    {
        var lines = new List<string> { Header };
        lines.AddRange(all.Select(ToCsvLine));
        File.WriteAllLines(_filePath, lines);
    }

    private static Administrator? TryParse(string line)
    {
        var f = CsvUtils.SplitLine(line);
        if (f.Length < 5) return null;
        if (!int.TryParse(f[0], out var id)) return null;

        return new Administrator
        {
            Id = id,
            Username = f[1],
            Email = f[2],
            PasswordHash = f[3],
            AdminLevel = string.IsNullOrEmpty(f[4]) ? "Standard" : f[4]
        };
    }

    private static string ToCsvLine(Administrator a) => CsvUtils.JoinFields(new[]
    {
        a.Id.ToString(), a.Username, a.Email, a.PasswordHash, a.AdminLevel
    });
}
