using SIMS.Domain;

namespace SIMS.Application;

// Facade: a single stable entry point the presentation layer calls to enrol a
// student, hiding the multi-service coordination behind one method. It only
// coordinates — every validation rule stays inside the service that owns it
// (capacity in CourseService, duplicate guard in EnrollmentService), so the
// facade never accumulates business logic of its own and does not become a
// God Object.
public class SimsFacade
{
    private readonly StudentService _studentService;
    private readonly CourseService _courseService;
    private readonly EnrollmentService _enrollmentService;
    private readonly IEmailService _emailService;

    public SimsFacade(
        StudentService studentService,
        CourseService courseService,
        EnrollmentService enrollmentService,
        IEmailService emailService)
    {
        _studentService = studentService;
        _courseService = courseService;
        _enrollmentService = enrollmentService;
        _emailService = emailService;
    }

    public Enrollment EnrollStudent(int studentId, int courseId)
    {
        var student = _studentService.GetProfile(studentId)
            ?? throw new InvalidOperationException($"Student {studentId} not found.");

        var course = _courseService.GetById(courseId)
            ?? throw new InvalidOperationException($"Course {courseId} not found.");

        if (!_courseService.HasAvailableSeats(courseId))
            throw new InvalidOperationException($"Course {course.Code} is full.");

        var enrollment = _enrollmentService.Enroll(studentId, courseId);
        _emailService.SendEnrolmentConfirmation(student, course);
        return enrollment;
    }
}
