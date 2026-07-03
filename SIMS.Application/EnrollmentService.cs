using SIMS.Domain;

namespace SIMS.Application;

// Owns enrolment records and grade submission. Keeps its own duplicate guard so
// it is safe to call independently of the facade.
public class EnrollmentService
{
    private readonly IEnrollmentRepository _enrollmentRepository;

    public EnrollmentService(IEnrollmentRepository enrollmentRepository)
    {
        _enrollmentRepository = enrollmentRepository;
    }

    public Enrollment Enroll(int studentId, int courseId)
    {
        if (_enrollmentRepository.Exists(studentId, courseId))
            throw new InvalidOperationException($"Student {studentId} is already enrolled in course {courseId}.");

        var enrollment = new Enrollment
        {
            StudentId = studentId,
            CourseId = courseId,
            EnrolmentDate = DateTime.UtcNow,
            Status = EnrollmentStatus.Active
        };

        _enrollmentRepository.Add(enrollment);
        return enrollment;
    }

    public IEnumerable<Enrollment> GetStudentEnrollments(int studentId) =>
        _enrollmentRepository.GetByStudent(studentId);

    public IEnumerable<Enrollment> GetCourseEnrollments(int courseId) =>
        _enrollmentRepository.GetByCourse(courseId);

    public void SubmitGrade(int enrollmentId, double grade)
    {
        if (grade < 0 || grade > 10)
            throw new ArgumentOutOfRangeException(nameof(grade), "Grade must be between 0 and 10.");

        var enrollment = _enrollmentRepository.GetById(enrollmentId)
            ?? throw new InvalidOperationException($"Enrollment {enrollmentId} not found.");

        enrollment.Grade = grade;
        enrollment.Status = EnrollmentStatus.Completed;
        _enrollmentRepository.Update(enrollment);
    }
}
