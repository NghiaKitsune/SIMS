using SIMS.Domain;

namespace SIMS.Infrastructure;

// Infrastructure-layer implementation of IStudentRepository. Uses
// File.ReadLines() so large CSV files are streamed line-by-line (LINQ
// deferred execution) instead of being loaded into memory all at once.
public class CsvStudentRepository : IStudentRepository
{
    private const string Header = "Id,Username,Email,PasswordHash,StudentId,AcademicYear";
    private readonly string _filePath;

    public CsvStudentRepository(string filePath)
    {
        _filePath = filePath;
        CsvFile.EnsureExists(_filePath, Header);
    }

    public IEnumerable<Student> GetAll() =>
        File.ReadLines(_filePath)
            .Skip(1)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(TryParse)
            .Where(s => s is not null)!;

    public Student? GetById(int id) => GetAll().FirstOrDefault(s => s.Id == id);

    public Student? GetByUsername(string username) =>
        GetAll().FirstOrDefault(s => string.Equals(s.Username, username, StringComparison.OrdinalIgnoreCase));

    public void Add(Student student)
    {
        var students = GetAll().ToList();
        student.Id = students.Count == 0 ? 1 : students.Max(s => s.Id) + 1;
        students.Add(student);
        WriteAll(students);
    }

    public void Update(Student student)
    {
        var students = GetAll().ToList();
        var index = students.FindIndex(s => s.Id == student.Id);
        if (index == -1) throw new InvalidOperationException($"Student {student.Id} not found.");
        students[index] = student;
        WriteAll(students);
    }

    public void Delete(int id)
    {
        var students = GetAll().Where(s => s.Id != id).ToList();
        WriteAll(students);
    }

    private void WriteAll(IEnumerable<Student> students)
    {
        var lines = new List<string> { Header };
        lines.AddRange(students.Select(ToCsvLine));
        File.WriteAllLines(_filePath, lines);
    }

    // Malformed rows (wrong column count, unparsable Id) are skipped rather
    // than throwing, so one corrupt line cannot break GetAll() for the rest
    // of the dataset.
    private static Student? TryParse(string line)
    {
        var f = CsvUtils.SplitLine(line);
        if (f.Length < 6) return null;
        if (!int.TryParse(f[0], out var id)) return null;

        return new Student
        {
            Id = id,
            Username = f[1],
            Email = f[2],
            PasswordHash = f[3],
            StudentId = f[4],
            AcademicYear = f[5]
        };
    }

    private static string ToCsvLine(Student s) => CsvUtils.JoinFields(new[]
    {
        s.Id.ToString(), s.Username, s.Email, s.PasswordHash, s.StudentId, s.AcademicYear
    });
}
