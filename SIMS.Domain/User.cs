namespace SIMS.Domain;

public enum UserRole
{
    Student,
    Faculty,
    Administrator
}

public abstract class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; protected set; }

    public abstract bool Login(string password);

    protected bool VerifyPassword(string password) => PasswordHashing.Verify(password, PasswordHash);
}
