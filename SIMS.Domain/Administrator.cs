namespace SIMS.Domain;

public class Administrator : User
{
    public string AdminLevel { get; set; } = "Standard";

    public Administrator()
    {
        Role = UserRole.Administrator;
    }

    public override bool Login(string password) => VerifyPassword(password);
}
