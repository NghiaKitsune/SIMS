using SIMS.Domain;

namespace SIMS.Application;

// Authenticates every user type through a single method. Because Student,
// Faculty and Administrator all honour the User.Login contract (LSP), this
// service never needs to branch on the concrete type — it looks the user up,
// then delegates verification to the polymorphic Login method.
public class AuthenticationService
{
    private readonly IStudentRepository _studentRepository;
    private readonly IFacultyRepository _facultyRepository;
    private readonly IAdministratorRepository _administratorRepository;

    public AuthenticationService(
        IStudentRepository studentRepository,
        IFacultyRepository facultyRepository,
        IAdministratorRepository administratorRepository)
    {
        _studentRepository = studentRepository;
        _facultyRepository = facultyRepository;
        _administratorRepository = administratorRepository;
    }

    public User? Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return null;

        var user = FindByUsername(username);
        if (user is null) return null;

        return user.Login(password) ? user : null;
    }

    private User? FindByUsername(string username) =>
        _studentRepository.GetByUsername(username)
        ?? (User?)_facultyRepository.GetByUsername(username)
        ?? _administratorRepository.GetByUsername(username);
}
