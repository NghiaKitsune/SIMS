using SIMS.Domain;

namespace SIMS.Infrastructure;

public class CsvCourseRepository : ICourseRepository
{
    private const string Header = "Id,Code,Title,Credits,Capacity";
    private readonly string _filePath;

    public CsvCourseRepository(string filePath)
    {
        _filePath = filePath;
        CsvFile.EnsureExists(_filePath, Header);
    }

    public IEnumerable<Course> GetAll() =>
        File.ReadLines(_filePath)
            .Skip(1)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(TryParse)
            .Where(c => c is not null)!;

    public Course? GetById(int id) => GetAll().FirstOrDefault(c => c.Id == id);

    public void Add(Course course)
    {
        var all = GetAll().ToList();
        course.Id = all.Count == 0 ? 1 : all.Max(c => c.Id) + 1;
        all.Add(course);
        WriteAll(all);
    }

    public void Update(Course course)
    {
        var all = GetAll().ToList();
        var index = all.FindIndex(c => c.Id == course.Id);
        if (index == -1) throw new InvalidOperationException($"Course {course.Id} not found.");
        all[index] = course;
        WriteAll(all);
    }

    public void Delete(int id)
    {
        var all = GetAll().Where(c => c.Id != id).ToList();
        WriteAll(all);
    }

    private void WriteAll(IEnumerable<Course> all)
    {
        var lines = new List<string> { Header };
        lines.AddRange(all.Select(ToCsvLine));
        File.WriteAllLines(_filePath, lines);
    }

    private static Course? TryParse(string line)
    {
        var f = CsvUtils.SplitLine(line);
        if (f.Length < 5) return null;
        if (!int.TryParse(f[0], out var id)) return null;
        if (!int.TryParse(f[3], out var credits)) return null;
        if (!int.TryParse(f[4], out var capacity)) return null;

        return new Course { Id = id, Code = f[1], Title = f[2], Credits = credits, Capacity = capacity };
    }

    private static string ToCsvLine(Course c) => CsvUtils.JoinFields(new[]
    {
        c.Id.ToString(), c.Code, c.Title, c.Credits.ToString(), c.Capacity.ToString()
    });
}
