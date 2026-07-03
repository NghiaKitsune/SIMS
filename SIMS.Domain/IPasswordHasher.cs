namespace SIMS.Domain;

// DIP: application services depend on this abstraction, not on a concrete
// hashing algorithm. SIMS.Infrastructure supplies the PBKDF2 implementation.
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}
