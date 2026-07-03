namespace SIMS.Domain;

public interface IFacultyRepository
{
    Faculty? GetById(int id);
    Faculty? GetByUsername(string username);
    IEnumerable<Faculty> GetAll();
    void Add(Faculty faculty);
    void Update(Faculty faculty);
    void Delete(int id);
}
