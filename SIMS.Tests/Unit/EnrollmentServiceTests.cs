using Moq;
using SIMS.Application;
using SIMS.Domain;

namespace SIMS.Tests.Unit;

// EnrollmentService owns the enrolment records: a duplicate guard on Enroll and
// the grade-range rule on SubmitGrade.
public class EnrollmentServiceTests
{
    private readonly Mock<IEnrollmentRepository> _repo = new();

    private EnrollmentService CreateSut() => new(_repo.Object);

    [Fact]
    public void Enroll_NewEnrolment_AddsActiveRecord()
    {
        _repo.Setup(r => r.Exists(1, 10)).Returns(false);
        var sut = CreateSut();

        var enrolment = sut.Enroll(1, 10);

        Assert.Equal(1, enrolment.StudentId);
        Assert.Equal(10, enrolment.CourseId);
        Assert.Equal(EnrollmentStatus.Active, enrolment.Status);
        _repo.Verify(r => r.Add(It.IsAny<Enrollment>()), Times.Once);
    }

    [Fact]
    public void Enroll_Duplicate_ThrowsAndDoesNotAdd()
    {
        _repo.Setup(r => r.Exists(1, 10)).Returns(true);
        var sut = CreateSut();

        Assert.Throws<InvalidOperationException>(() => sut.Enroll(1, 10));
        _repo.Verify(r => r.Add(It.IsAny<Enrollment>()), Times.Never);
    }

    [Fact]
    public void SubmitGrade_ValidGrade_SetsGradeAndCompletesAndUpdates()
    {
        var enrolment = new Enrollment { Id = 7, Status = EnrollmentStatus.Active };
        _repo.Setup(r => r.GetById(7)).Returns(enrolment);
        var sut = CreateSut();

        sut.SubmitGrade(7, 8.5);

        Assert.Equal(8.5, enrolment.Grade);
        Assert.Equal(EnrollmentStatus.Completed, enrolment.Status);
        _repo.Verify(r => r.Update(enrolment), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public void SubmitGrade_BoundaryGrades_AreAccepted(double grade)
    {
        var enrolment = new Enrollment { Id = 7, Status = EnrollmentStatus.Active };
        _repo.Setup(r => r.GetById(7)).Returns(enrolment);
        var sut = CreateSut();

        sut.SubmitGrade(7, grade);

        Assert.Equal(grade, enrolment.Grade);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(10.1)]
    [InlineData(100)]
    public void SubmitGrade_OutOfRange_ThrowsAndDoesNotUpdate(double grade)
    {
        var sut = CreateSut();

        Assert.Throws<ArgumentOutOfRangeException>(() => sut.SubmitGrade(7, grade));
        _repo.Verify(r => r.Update(It.IsAny<Enrollment>()), Times.Never);
    }

    [Fact]
    public void SubmitGrade_UnknownEnrolment_Throws()
    {
        _repo.Setup(r => r.GetById(7)).Returns((Enrollment?)null);
        var sut = CreateSut();

        Assert.Throws<InvalidOperationException>(() => sut.SubmitGrade(7, 5.0));
    }
}
