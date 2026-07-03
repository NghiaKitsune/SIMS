using System.Globalization;
using SIMS.Domain;

namespace SIMS.Infrastructure;

public class CsvEnrollmentRepository : IEnrollmentRepository
{
    private const string Header = "Id,StudentId,CourseId,EnrolmentDate,Grade,Status";
    private readonly string _filePath;

    public CsvEnrollmentRepository(string filePath)
    {
        _filePath = filePath;
        CsvFile.EnsureExists(_filePath, Header);
    }

    public IEnumerable<Enrollment> GetAll() =>
        File.ReadLines(_filePath)
            .Skip(1)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(TryParse)
            .Where(e => e is not null)!;

    public Enrollment? GetById(int id) => GetAll().FirstOrDefault(e => e.Id == id);

    public IEnumerable<Enrollment> GetByStudent(int studentId) =>
        GetAll().Where(e => e.StudentId == studentId);

    public IEnumerable<Enrollment> GetByCourse(int courseId) =>
        GetAll().Where(e => e.CourseId == courseId);

    public bool Exists(int studentId, int courseId) =>
        GetAll().Any(e => e.StudentId == studentId && e.CourseId == courseId && e.Status != EnrollmentStatus.Withdrawn);

    public void Add(Enrollment enrollment)
    {
        var all = GetAll().ToList();
        enrollment.Id = all.Count == 0 ? 1 : all.Max(e => e.Id) + 1;
        all.Add(enrollment);
        WriteAll(all);
    }

    public void Update(Enrollment enrollment)
    {
        var all = GetAll().ToList();
        var index = all.FindIndex(e => e.Id == enrollment.Id);
        if (index == -1) throw new InvalidOperationException($"Enrollment {enrollment.Id} not found.");
        all[index] = enrollment;
        WriteAll(all);
    }

    private void WriteAll(IEnumerable<Enrollment> all)
    {
        var lines = new List<string> { Header };
        lines.AddRange(all.Select(ToCsvLine));
        File.WriteAllLines(_filePath, lines);
    }

    private static Enrollment? TryParse(string line)
    {
        var f = CsvUtils.SplitLine(line);
        if (f.Length < 6) return null;
        if (!int.TryParse(f[0], out var id)) return null;
        if (!int.TryParse(f[1], out var studentId)) return null;
        if (!int.TryParse(f[2], out var courseId)) return null;
        if (!DateTime.TryParse(f[3], CultureInfo.InvariantCulture, DateTimeStyles.None, out var enrolmentDate)) return null;
        if (!Enum.TryParse<EnrollmentStatus>(f[5], out var status)) return null;

        double? grade = double.TryParse(f[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var g) ? g : null;

        return new Enrollment
        {
            Id = id,
            StudentId = studentId,
            CourseId = courseId,
            EnrolmentDate = enrolmentDate,
            Grade = grade,
            Status = status
        };
    }

    private static string ToCsvLine(Enrollment e) => CsvUtils.JoinFields(new[]
    {
        e.Id.ToString(),
        e.StudentId.ToString(),
        e.CourseId.ToString(),
        e.EnrolmentDate.ToString("o", CultureInfo.InvariantCulture),
        e.Grade?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
        e.Status.ToString()
    });
}
