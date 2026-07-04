using Moq;
using SIMS.Application;
using SIMS.Domain;

namespace SIMS.Tests.Unit;

// The facade must only *coordinate*: it delegates the capacity rule to
// CourseService and the duplicate guard to EnrollmentService, holding no
// business logic of its own. The concrete services have non-virtual methods, so
// they are assembled over Moq repositories (rather than mocked directly) and the
// facade's behaviour is verified at the repository / email boundary.
public class SimsFacadeTests
{
    private readonly Mock<IStudentRepository> _students = new();
    private readonly Mock<ICourseRepository> _courses = new();
    private readonly Mock<IEnrollmentRepository> _enrolments = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IEmailService> _email = new();

    private SimsFacade CreateSut()
    {
        var studentService = new StudentService(_students.Object, _hasher.Object, _email.Object);
        var courseService = new CourseService(_courses.Object, _enrolments.Object);
        var enrollmentService = new EnrollmentService(_enrolments.Object);
        return new SimsFacade(studentService, courseService, enrollmentService, _email.Object);
    }

    [Fact]
    public void EnrollStudent_SeatsAvailable_EnrolsAndSendsConfirmation()
    {
        _students.Setup(r => r.GetById(1)).Returns(new Student { Id = 1, Username = "nvana" });
        _courses.Setup(r => r.GetById(10)).Returns(new Course { Id = 10, Code = "CS101", Capacity = 30 });
        _enrolments.Setup(r => r.GetByCourse(10)).Returns(Array.Empty<Enrollment>()); // 0 of 30 taken
        _enrolments.Setup(r => r.Exists(1, 10)).Returns(false);
        var sut = CreateSut();

        var enrolment = sut.EnrollStudent(1, 10);

        Assert.Equal(1, enrolment.StudentId);
        Assert.Equal(10, enrolment.CourseId);
        _enrolments.Verify(r => r.Add(It.IsAny<Enrollment>()), Times.Once);
        _email.Verify(e => e.SendEnrolmentConfirmation(It.IsAny<Student>(), It.IsAny<Course>()), Times.Once);
    }

    [Fact]
    public void EnrollStudent_CourseFull_ThrowsAndNeverEnrolsOrEmails()
    {
        _students.Setup(r => r.GetById(1)).Returns(new Student { Id = 1, Username = "nvana" });
        _courses.Setup(r => r.GetById(10)).Returns(new Course { Id = 10, Code = "CS101", Capacity = 1 });
        _enrolments.Setup(r => r.GetByCourse(10)).Returns(new[]
        {
            new Enrollment { Status = EnrollmentStatus.Active } // 1 of 1 taken -> full
        });
        var sut = CreateSut();

        Assert.Throws<InvalidOperationException>(() => sut.EnrollStudent(1, 10));

        // The rule lives in CourseService: because it reported "full", the facade
        // stopped before touching EnrollmentService.Enroll or the email service.
        _enrolments.Verify(r => r.Add(It.IsAny<Enrollment>()), Times.Never);
        _email.Verify(e => e.SendEnrolmentConfirmation(It.IsAny<Student>(), It.IsAny<Course>()), Times.Never);
    }

    [Fact]
    public void EnrollStudent_UnknownStudent_ThrowsAndDoesNotEnrol()
    {
        _students.Setup(r => r.GetById(1)).Returns((Student?)null);
        var sut = CreateSut();

        Assert.Throws<InvalidOperationException>(() => sut.EnrollStudent(1, 10));
        _enrolments.Verify(r => r.Add(It.IsAny<Enrollment>()), Times.Never);
    }

    [Fact]
    public void EnrollStudent_UnknownCourse_ThrowsAndDoesNotEnrol()
    {
        _students.Setup(r => r.GetById(1)).Returns(new Student { Id = 1 });
        _courses.Setup(r => r.GetById(10)).Returns((Course?)null);
        var sut = CreateSut();

        Assert.Throws<InvalidOperationException>(() => sut.EnrollStudent(1, 10));
        _enrolments.Verify(r => r.Add(It.IsAny<Enrollment>()), Times.Never);
    }
}
