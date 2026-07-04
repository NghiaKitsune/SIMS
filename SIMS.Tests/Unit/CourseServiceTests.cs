using Moq;
using SIMS.Application;
using SIMS.Domain;

namespace SIMS.Tests.Unit;

// CourseService owns catalogue creation validation and the capacity rule. The
// capacity check counts only Active enrolments against Course.Capacity.
public class CourseServiceTests
{
    private readonly Mock<ICourseRepository> _courses = new();
    private readonly Mock<IEnrollmentRepository> _enrolments = new();

    private CourseService CreateSut() => new(_courses.Object, _enrolments.Object);

    [Fact]
    public void Create_ValidInput_AddsCourse()
    {
        var sut = CreateSut();

        var course = sut.Create("CS101", "Intro", 3, 30);

        Assert.Equal("CS101", course.Code);
        _courses.Verify(r => r.Add(It.IsAny<Course>()), Times.Once);
    }

    [Theory]
    [InlineData("", 3, 30)]     // blank code
    [InlineData("  ", 3, 30)]   // whitespace code
    [InlineData("CS101", 0, 30)]  // credits not positive
    [InlineData("CS101", -1, 30)] // negative credits
    [InlineData("CS101", 3, 0)]   // capacity not positive
    [InlineData("CS101", 3, -5)]  // negative capacity
    public void Create_InvalidInput_ThrowsArgumentExceptionAndDoesNotAdd(string code, int credits, int capacity)
    {
        var sut = CreateSut();

        Assert.Throws<ArgumentException>(() => sut.Create(code, "Title", credits, capacity));
        _courses.Verify(r => r.Add(It.IsAny<Course>()), Times.Never);
    }

    [Fact]
    public void HasAvailableSeats_BelowCapacity_ReturnsTrue()
    {
        _courses.Setup(r => r.GetById(1)).Returns(new Course { Id = 1, Capacity = 2 });
        _enrolments.Setup(r => r.GetByCourse(1)).Returns(new[]
        {
            new Enrollment { Status = EnrollmentStatus.Active }
        });
        var sut = CreateSut();

        Assert.True(sut.HasAvailableSeats(1));
    }

    [Fact]
    public void HasAvailableSeats_AtCapacity_ReturnsFalse()
    {
        _courses.Setup(r => r.GetById(1)).Returns(new Course { Id = 1, Capacity = 2 });
        _enrolments.Setup(r => r.GetByCourse(1)).Returns(new[]
        {
            new Enrollment { Status = EnrollmentStatus.Active },
            new Enrollment { Status = EnrollmentStatus.Active }
        });
        var sut = CreateSut();

        Assert.False(sut.HasAvailableSeats(1));
    }

    [Fact]
    public void HasAvailableSeats_WithdrawnAndCompletedDoNotConsumeSeats()
    {
        _courses.Setup(r => r.GetById(1)).Returns(new Course { Id = 1, Capacity = 2 });
        _enrolments.Setup(r => r.GetByCourse(1)).Returns(new[]
        {
            new Enrollment { Status = EnrollmentStatus.Active },
            new Enrollment { Status = EnrollmentStatus.Withdrawn },
            new Enrollment { Status = EnrollmentStatus.Completed }
        });
        var sut = CreateSut();

        // Only the single Active enrolment counts, so a seat remains.
        Assert.True(sut.HasAvailableSeats(1));
    }

    [Fact]
    public void HasAvailableSeats_UnknownCourse_Throws()
    {
        _courses.Setup(r => r.GetById(99)).Returns((Course?)null);
        var sut = CreateSut();

        Assert.Throws<InvalidOperationException>(() => sut.HasAvailableSeats(99));
    }
}
