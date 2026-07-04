using Moq;
using SIMS.Application;
using SIMS.Domain;

namespace SIMS.Tests.Unit;

// The registration example the Assignment 1 report explicitly commits to.
// StudentService is exercised in isolation: its three collaborators
// (IStudentRepository, IPasswordHasher, IEmailService) are Moq mocks, so the
// test asserts behaviour and interactions, not persistence.
public class StudentServiceTests
{
    private readonly Mock<IStudentRepository> _repo = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IEmailService> _email = new();

    private StudentService CreateSut() => new(_repo.Object, _hasher.Object, _email.Object);

    [Fact]
    public void Register_ValidInput_AddsStudentHashesPasswordAndSendsEmail()
    {
        _repo.Setup(r => r.GetByUsername("nvana")).Returns((Student?)null);
        _repo.Setup(r => r.GetAll()).Returns(Array.Empty<Student>());
        _hasher.Setup(h => h.Hash("Student@123")).Returns("HASHED::Student@123");
        var sut = CreateSut();

        var result = sut.Register("nvana", "nvana@student.sims.edu", "Student@123", "BC00001", "2025-2026");

        // Persisted exactly once.
        _repo.Verify(r => r.Add(It.IsAny<Student>()), Times.Once);
        // Password is hashed, never stored in plaintext.
        _hasher.Verify(h => h.Hash("Student@123"), Times.Once);
        Assert.Equal("HASHED::Student@123", result.PasswordHash);
        Assert.NotEqual("Student@123", result.PasswordHash);
        // Confirmation email fired for the created student.
        _email.Verify(e => e.SendRegistrationConfirmation(It.Is<Student>(s => s.Username == "nvana")), Times.Once);
    }

    [Fact]
    public void Register_DuplicateUsername_ThrowsAndDoesNotAdd()
    {
        _repo.Setup(r => r.GetByUsername("nvana")).Returns(new Student { Username = "nvana" });
        var sut = CreateSut();

        Assert.Throws<InvalidOperationException>(
            () => sut.Register("nvana", "e@x.com", "pw", "BC00002", "2025-2026"));

        _repo.Verify(r => r.Add(It.IsAny<Student>()), Times.Never);
        _email.Verify(e => e.SendRegistrationConfirmation(It.IsAny<Student>()), Times.Never);
    }

    [Fact]
    public void Register_DuplicateStudentId_ThrowsAndDoesNotAdd()
    {
        _repo.Setup(r => r.GetByUsername(It.IsAny<string>())).Returns((Student?)null);
        _repo.Setup(r => r.GetAll()).Returns(new[] { new Student { StudentId = "BC00001" } });
        var sut = CreateSut();

        Assert.Throws<InvalidOperationException>(
            () => sut.Register("someoneelse", "e@x.com", "pw", "BC00001", "2025-2026"));

        _repo.Verify(r => r.Add(It.IsAny<Student>()), Times.Never);
    }

    [Fact]
    public void Register_DuplicateStudentId_IsCaseInsensitive()
    {
        _repo.Setup(r => r.GetByUsername(It.IsAny<string>())).Returns((Student?)null);
        _repo.Setup(r => r.GetAll()).Returns(new[] { new Student { StudentId = "bc00001" } });
        var sut = CreateSut();

        Assert.Throws<InvalidOperationException>(
            () => sut.Register("newuser", "e@x.com", "pw", "BC00001", "2025-2026"));
    }

    [Theory]
    [InlineData("", "pw", "BC1")]      // blank username
    [InlineData("  ", "pw", "BC1")]    // whitespace username
    [InlineData("user", "", "BC1")]    // blank password
    [InlineData("user", "pw", "")]     // blank student id
    public void Register_BlankRequiredField_ThrowsArgumentException(string username, string password, string studentId)
    {
        var sut = CreateSut();

        Assert.Throws<ArgumentException>(
            () => sut.Register(username, "e@x.com", password, studentId, "2025-2026"));

        _repo.Verify(r => r.Add(It.IsAny<Student>()), Times.Never);
    }
}
