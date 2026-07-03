using SIMS.Domain;

namespace SIMS.Infrastructure;

public class Pbkdf2PasswordHasher : IPasswordHasher
{
    public string Hash(string password) => PasswordHashing.Hash(password);

    public bool Verify(string password, string passwordHash) => PasswordHashing.Verify(password, passwordHash);
}
