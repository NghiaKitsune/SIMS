using SIMS.Domain;
using SIMS.Infrastructure;

namespace SIMS.Tests.Integration;

// Integration tests that exercise the CSV repositories against real files in a
// throwaway temp directory. They lock in the two properties CLAUDE.md requires:
// a field containing a comma round-trips through CsvUtils quoting, and a
// malformed row is skipped without throwing.
public class CsvRepositoryTests : IDisposable
{
    private readonly string _dir;

    public CsvRepositoryTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "sims-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    private string Path_(string file) => Path.Combine(_dir, file);

    // --- Students ---------------------------------------------------------

    [Fact]
    public void EnsureExists_CreatesFileWithHeaderOnConstruction()
    {
        var path = Path_("students.csv");
        _ = new CsvStudentRepository(path);

        Assert.True(File.Exists(path));
        Assert.Equal("Id,Username,Email,PasswordHash,StudentId,AcademicYear", File.ReadLines(path).First());
    }

    [Fact]
    public void Add_AssignsIncrementingIdsAndAppends()
    {
        var repo = new CsvStudentRepository(Path_("students.csv"));

        repo.Add(new Student { Username = "a", StudentId = "S1" });
        repo.Add(new Student { Username = "b", StudentId = "S2" });

        var all = repo.GetAll().OrderBy(s => s.Id).ToList();
        Assert.Equal(2, all.Count);
        Assert.Equal(1, all[0].Id);
        Assert.Equal(2, all[1].Id);
    }

    [Fact]
    public void Update_DoesNotCorruptNeighbouringRows()
    {
        var repo = new CsvStudentRepository(Path_("students.csv"));
        repo.Add(new Student { Username = "a", StudentId = "S1", AcademicYear = "2024" });
        repo.Add(new Student { Username = "b", StudentId = "S2", AcademicYear = "2024" });

        var b = repo.GetByUsername("b")!;
        b.AcademicYear = "2025";
        repo.Update(b);

        var a = repo.GetByUsername("a")!;
        Assert.Equal("2024", a.AcademicYear);            // neighbour untouched
        Assert.Equal("2025", repo.GetByUsername("b")!.AcademicYear);
        Assert.Equal(2, repo.GetAll().Count());
    }

    [Fact]
    public void Delete_RemovesOnlyTheTargetRow()
    {
        var repo = new CsvStudentRepository(Path_("students.csv"));
        repo.Add(new Student { Username = "a", StudentId = "S1" });
        repo.Add(new Student { Username = "b", StudentId = "S2" });

        var a = repo.GetByUsername("a")!;
        repo.Delete(a.Id);

        var all = repo.GetAll().ToList();
        Assert.Single(all);
        Assert.Equal("b", all[0].Username);
    }

    [Fact]
    public void GetAll_SkipsMalformedRowsWithoutThrowing()
    {
        var path = Path_("students.csv");
        File.WriteAllLines(path, new[]
        {
            "Id,Username,Email,PasswordHash,StudentId,AcademicYear",
            "1,alice,alice@x.com,hash,S1,2025",
            "not-an-int,broken,row",           // unparsable id + too few columns
            "",                                  // blank line
            "2,bob,bob@x.com,hash,S2,2025"
        });
        var repo = new CsvStudentRepository(path);

        var all = repo.GetAll().ToList();   // must not throw

        Assert.Equal(2, all.Count);
        Assert.Contains(all, s => s.Username == "alice");
        Assert.Contains(all, s => s.Username == "bob");
    }

    // --- Courses: comma-in-field quoting ---------------------------------

    [Fact]
    public void Course_FieldContainingComma_RoundTripsViaQuoting()
    {
        var path = Path_("courses.csv");
        var repo = new CsvCourseRepository(path);
        repo.Add(new Course { Code = "SE301", Title = "Design, Analysis, and Patterns", Credits = 4, Capacity = 20 });

        // Reload from a fresh instance to prove it survived the file round-trip.
        var reloaded = new CsvCourseRepository(path).GetAll().Single();
        Assert.Equal("Design, Analysis, and Patterns", reloaded.Title);
        Assert.Equal(4, reloaded.Credits);
        Assert.Equal(20, reloaded.Capacity);
    }

    // --- Enrolments: empty optional column (Grade) -----------------------

    [Fact]
    public void Enrollment_EmptyOptionalGrade_RoundTripsAsNull()
    {
        var path = Path_("enrollments.csv");
        var repo = new CsvEnrollmentRepository(path);
        repo.Add(new Enrollment { StudentId = 1, CourseId = 2, EnrolmentDate = DateTime.UtcNow, Grade = null, Status = EnrollmentStatus.Active });

        var reloaded = new CsvEnrollmentRepository(path).GetAll().Single();
        Assert.Null(reloaded.Grade);
        Assert.Equal(EnrollmentStatus.Active, reloaded.Status);
    }

    [Fact]
    public void Enrollment_GradeValue_RoundTrips()
    {
        var path = Path_("enrollments.csv");
        var repo = new CsvEnrollmentRepository(path);
        repo.Add(new Enrollment { StudentId = 1, CourseId = 2, EnrolmentDate = DateTime.UtcNow, Grade = 8.5, Status = EnrollmentStatus.Completed });

        var reloaded = new CsvEnrollmentRepository(path).GetAll().Single();
        Assert.Equal(8.5, reloaded.Grade);
        Assert.Equal(EnrollmentStatus.Completed, reloaded.Status);
    }
}
