using SIMS.Domain;

namespace SIMS.Infrastructure;

// Idempotent seed data so the application is usable on first run: one
// Administrator account to log in with, plus a sample Faculty, Student and
// a few Courses. Runs once per empty CSV file; safe to call on every startup.
public static class DataSeeder
{
    public const string SeedAdminUsername = "admin";
    public const string SeedAdminPassword = "Admin@123";
    public const string SeedFacultyUsername = "jsmith";
    public const string SeedFacultyPassword = "Faculty@123";
    public const string SeedStudentUsername = "nvana";
    public const string SeedStudentPassword = "Student@123";

    public static void SeedIfEmpty(
        IStudentRepository studentRepository,
        IFacultyRepository facultyRepository,
        IAdministratorRepository administratorRepository,
        ICourseRepository courseRepository,
        IPasswordHasher passwordHasher)
    {
        if (!administratorRepository.GetAll().Any())
        {
            administratorRepository.Add(new Administrator
            {
                Username = SeedAdminUsername,
                Email = "admin@sims.edu",
                PasswordHash = passwordHasher.Hash(SeedAdminPassword),
                AdminLevel = "Standard"
            });
        }

        if (!facultyRepository.GetAll().Any())
        {
            facultyRepository.Add(new Faculty
            {
                Username = SeedFacultyUsername,
                Email = "jsmith@sims.edu",
                PasswordHash = passwordHasher.Hash(SeedFacultyPassword),
                EmployeeId = "EMP001",
                Department = "Computing"
            });
        }

        if (!studentRepository.GetAll().Any())
        {
            studentRepository.Add(new Student
            {
                Username = SeedStudentUsername,
                Email = "nvana@student.sims.edu",
                PasswordHash = passwordHasher.Hash(SeedStudentPassword),
                StudentId = "BC00001",
                AcademicYear = "2025-2026"
            });
        }

        if (!courseRepository.GetAll().Any())
        {
            courseRepository.Add(new Course { Code = "CS101", Title = "Introduction to Programming", Credits = 3, Capacity = 30 });
            courseRepository.Add(new Course { Code = "CS201", Title = "Data Structures and Algorithms", Credits = 4, Capacity = 25 });
            courseRepository.Add(new Course { Code = "SE301", Title = "Applied Programming and Design Principles", Credits = 4, Capacity = 20 });
        }
    }
}
