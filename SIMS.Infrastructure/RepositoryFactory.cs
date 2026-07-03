using SIMS.Domain;

namespace SIMS.Infrastructure;

public enum StorageType
{
    Csv
    // Sql could be added later without changing any caller of this factory (OCP).
}

// Factory Method: centralises the decision of which IStudentRepository
// implementation to construct, so swapping storage technology (e.g. adding a
// SqlStudentRepository later) touches only this one class, not Program.cs
// or StudentService.
public static class RepositoryFactory
{
    public static IStudentRepository CreateStudentRepository(StorageType type, string path, bool withCache = true)
    {
        IStudentRepository repository = type switch
        {
            StorageType.Csv => new CsvStudentRepository(path),
            _ => throw new NotSupportedException($"Storage type '{type}' is not supported.")
        };

        return withCache ? new CachingStudentRepository(repository) : repository;
    }
}
