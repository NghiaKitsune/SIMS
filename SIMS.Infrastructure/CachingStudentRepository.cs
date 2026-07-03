using SIMS.Domain;

namespace SIMS.Infrastructure;

// Decorator pattern: adds an in-memory read cache in front of any
// IStudentRepository (typically CsvStudentRepository) without modifying it
// or the IStudentRepository contract that StudentService depends on.
// Composed in Program.cs as: new CachingStudentRepository(new CsvStudentRepository(path))
public class CachingStudentRepository : IStudentRepository
{
    private readonly IStudentRepository _inner;
    private readonly Dictionary<int, Student> _cache = new();

    public CachingStudentRepository(IStudentRepository inner)
    {
        _inner = inner;
    }

    public Student? GetById(int id)
    {
        if (_cache.TryGetValue(id, out var cached)) return cached;

        var student = _inner.GetById(id);
        if (student is not null) _cache[id] = student;
        return student;
    }

    public Student? GetByUsername(string username) =>
        _cache.Values.FirstOrDefault(s => string.Equals(s.Username, username, StringComparison.OrdinalIgnoreCase))
        ?? _inner.GetByUsername(username);

    public IEnumerable<Student> GetAll()
    {
        var students = _inner.GetAll().ToList();
        foreach (var student in students) _cache[student.Id] = student;
        return students;
    }

    public void Add(Student student)
    {
        _inner.Add(student);
        _cache[student.Id] = student;
    }

    public void Update(Student student)
    {
        _inner.Update(student);
        _cache[student.Id] = student;
    }

    public void Delete(int id)
    {
        _inner.Delete(id);
        _cache.Remove(id);
    }
}
