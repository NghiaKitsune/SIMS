namespace SIMS.Domain;

public class Student : User
{
    public string StudentId { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;

    public Student()
    {
        Role = UserRole.Student;
    }

    public override bool Login(string password) => VerifyPassword(password);
}
