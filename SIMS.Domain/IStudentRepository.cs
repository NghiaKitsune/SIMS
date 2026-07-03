namespace SIMS.Domain;

// Data access contract owned by the Domain layer (DIP). StudentService depends
// only on this interface; SIMS.Infrastructure supplies CsvStudentRepository.
public interface IStudentRepository
{
    Student? GetById(int id);
    Student? GetByUsername(string username);
    IEnumerable<Student> GetAll();
    void Add(Student student);
    void Update(Student student);
    void Delete(int id);
}
