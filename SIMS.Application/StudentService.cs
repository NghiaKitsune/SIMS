using SIMS.Domain;

namespace SIMS.Application;

// Business logic for students only. It holds no data-access or email code of
// its own: persistence goes through IStudentRepository and confirmation goes
// through IEmailService (SRP + DIP, both injected via the constructor).
public class StudentService
{
    private readonly IStudentRepository _repository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;

    public StudentService(
        IStudentRepository repository,
        IPasswordHasher passwordHasher,
        IEmailService emailService)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
    }

    public Student Register(string username, string email, string password, string studentId, string academicYear)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required.", nameof(username));
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password is required.", nameof(password));
        if (string.IsNullOrWhiteSpace(studentId))
            throw new ArgumentException("Student ID is required.", nameof(studentId));

        // Duplicate-student guard: reject a username or student number already in use.
        if (_repository.GetByUsername(username) is not null)
            throw new InvalidOperationException($"Username '{username}' is already taken.");
        if (_repository.GetAll().Any(s => string.Equals(s.StudentId, studentId, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Student ID '{studentId}' is already registered.");

        var student = new Student
        {
            Username = username,
            Email = email,
            PasswordHash = _passwordHasher.Hash(password),
            StudentId = studentId,
            AcademicYear = academicYear
        };

        _repository.Add(student);
        _emailService.SendRegistrationConfirmation(student);
        return student;
    }

    public Student? GetProfile(int id) => _repository.GetById(id);

    public Student? GetByUsername(string username) => _repository.GetByUsername(username);

    public IEnumerable<Student> GetAll() => _repository.GetAll();

    public void Delete(int id) => _repository.Delete(id);
}
