namespace SIMS.Domain;

public class Faculty : User
{
    public string EmployeeId { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;

    public Faculty()
    {
        Role = UserRole.Faculty;
    }

    public override bool Login(string password) => VerifyPassword(password);
}
