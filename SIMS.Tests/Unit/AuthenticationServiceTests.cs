using Moq;
using SIMS.Application;
using SIMS.Domain;

namespace SIMS.Tests.Unit;

// AuthenticationService looks a user up across the three role repositories, then
// delegates password verification to the polymorphic User.Login (LSP). The tests
// seed a real PBKDF2 hash via PasswordHashing.Hash so verification runs for real.
public class AuthenticationServiceTests
{
    private readonly Mock<IStudentRepository> _students = new();
    private readonly Mock<IFacultyRepository> _faculty = new();
    private readonly Mock<IAdministratorRepository> _admins = new();

    private AuthenticationService CreateSut() => new(_students.Object, _faculty.Object, _admins.Object);

    [Fact]
    public void Login_ValidStudentCredentials_ReturnsUser()
    {
        var student = new Student { Id = 5, Username = "nvana", PasswordHash = PasswordHashing.Hash("Student@123") };
        _students.Setup(r => r.GetByUsername("nvana")).Returns(student);
        var sut = CreateSut();

        var result = sut.Login("nvana", "Student@123");

        Assert.NotNull(result);
        Assert.Same(student, result);
    }

    [Fact]
    public void Login_ValidFacultyCredentials_ReturnsUser_ViaPolymorphicLookup()
    {
        _students.Setup(r => r.GetByUsername(It.IsAny<string>())).Returns((Student?)null);
        var faculty = new Faculty { Id = 2, Username = "jsmith", PasswordHash = PasswordHashing.Hash("Faculty@123") };
        _faculty.Setup(r => r.GetByUsername("jsmith")).Returns(faculty);
        var sut = CreateSut();

        var result = sut.Login("jsmith", "Faculty@123");

        Assert.NotNull(result);
        Assert.Equal(UserRole.Faculty, result!.Role);
    }

    [Fact]
    public void Login_WrongPassword_ReturnsNull()
    {
        var student = new Student { Username = "nvana", PasswordHash = PasswordHashing.Hash("Student@123") };
        _students.Setup(r => r.GetByUsername("nvana")).Returns(student);
        var sut = CreateSut();

        var result = sut.Login("nvana", "WrongPassword");

        Assert.Null(result);
    }

    [Fact]
    public void Login_UnknownUsername_ReturnsNull()
    {
        _students.Setup(r => r.GetByUsername(It.IsAny<string>())).Returns((Student?)null);
        _faculty.Setup(r => r.GetByUsername(It.IsAny<string>())).Returns((Faculty?)null);
        _admins.Setup(r => r.GetByUsername(It.IsAny<string>())).Returns((Administrator?)null);
        var sut = CreateSut();

        var result = sut.Login("ghost", "whatever");

        Assert.Null(result);
    }

    [Theory]
    [InlineData("", "pw")]
    [InlineData("user", "")]
    [InlineData(" ", "pw")]
    public void Login_BlankCredentials_ReturnsNullWithoutHittingRepositories(string username, string password)
    {
        var sut = CreateSut();

        var result = sut.Login(username, password);

        Assert.Null(result);
        _students.Verify(r => r.GetByUsername(It.IsAny<string>()), Times.Never);
    }
}
