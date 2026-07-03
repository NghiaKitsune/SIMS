using SIMS.Domain;

namespace SIMS.Application;

// Administrator-only account administration for Faculty and Administrator
// users. Student self-registration lives in StudentService instead, so each
// service keeps a single responsibility.
public class UserManagementService
{
    private readonly IFacultyRepository _facultyRepository;
    private readonly IAdministratorRepository _administratorRepository;
    private readonly IPasswordHasher _passwordHasher;

    public UserManagementService(
        IFacultyRepository facultyRepository,
        IAdministratorRepository administratorRepository,
        IPasswordHasher passwordHasher)
    {
        _facultyRepository = facultyRepository;
        _administratorRepository = administratorRepository;
        _passwordHasher = passwordHasher;
    }

    public Faculty CreateFaculty(string username, string email, string password, string employeeId, string department)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required.", nameof(username));
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password is required.", nameof(password));
        if (_facultyRepository.GetByUsername(username) is not null)
            throw new InvalidOperationException($"Username '{username}' is already taken.");

        var faculty = new Faculty
        {
            Username = username,
            Email = email,
            PasswordHash = _passwordHasher.Hash(password),
            EmployeeId = employeeId,
            Department = department
        };
        _facultyRepository.Add(faculty);
        return faculty;
    }

    public Administrator CreateAdministrator(string username, string email, string password, string adminLevel)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required.", nameof(username));
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password is required.", nameof(password));
        if (_administratorRepository.GetByUsername(username) is not null)
            throw new InvalidOperationException($"Username '{username}' is already taken.");

        var administrator = new Administrator
        {
            Username = username,
            Email = email,
            PasswordHash = _passwordHasher.Hash(password),
            AdminLevel = string.IsNullOrWhiteSpace(adminLevel) ? "Standard" : adminLevel
        };
        _administratorRepository.Add(administrator);
        return administrator;
    }

    public IEnumerable<Faculty> GetAllFaculty() => _facultyRepository.GetAll();

    public IEnumerable<Administrator> GetAllAdministrators() => _administratorRepository.GetAll();
}
