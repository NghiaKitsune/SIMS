using SIMS.Domain;

namespace SIMS.Application;

public record CourseEnrolmentSummary(int CourseId, string CourseCode, string CourseTitle, int EnrolledCount, int Capacity);

public record InstitutionReport(int TotalStudents, int TotalCourses, int TotalActiveEnrolments, IReadOnlyList<CourseEnrolmentSummary> Courses);

public record StudentGradeRow(string CourseCode, string CourseTitle, double? Grade, EnrollmentStatus Status);

// Read-only reporting used by Faculty (per-course results) and Administrator
// (institution-wide totals). It composes the repositories rather than owning
// any persistence itself.
public class ReportService
{
    private readonly IStudentRepository _studentRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;

    public ReportService(
        IStudentRepository studentRepository,
        ICourseRepository courseRepository,
        IEnrollmentRepository enrollmentRepository)
    {
        _studentRepository = studentRepository;
        _courseRepository = courseRepository;
        _enrollmentRepository = enrollmentRepository;
    }

    public InstitutionReport GenerateInstitutionReport()
    {
        var courses = _courseRepository.GetAll().ToList();
        var summaries = courses
            .Select(c => new CourseEnrolmentSummary(
                c.Id, c.Code, c.Title,
                _enrollmentRepository.GetByCourse(c.Id).Count(e => e.Status != EnrollmentStatus.Withdrawn),
                c.Capacity))
            .ToList();

        return new InstitutionReport(
            TotalStudents: _studentRepository.GetAll().Count(),
            TotalCourses: courses.Count,
            TotalActiveEnrolments: _enrollmentRepository.GetAll().Count(e => e.Status == EnrollmentStatus.Active),
            Courses: summaries);
    }

    public IReadOnlyList<StudentGradeRow> GenerateStudentReport(int studentId)
    {
        var courses = _courseRepository.GetAll().ToDictionary(c => c.Id);
        return _enrollmentRepository.GetByStudent(studentId)
            .Select(e =>
            {
                courses.TryGetValue(e.CourseId, out var course);
                return new StudentGradeRow(course?.Code ?? "?", course?.Title ?? "?", e.Grade, e.Status);
            })
            .ToList();
    }
}
