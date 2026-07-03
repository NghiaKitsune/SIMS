namespace SIMS.Domain;

public interface IAdministratorRepository
{
    Administrator? GetById(int id);
    Administrator? GetByUsername(string username);
    IEnumerable<Administrator> GetAll();
    void Add(Administrator administrator);
    void Update(Administrator administrator);
    void Delete(int id);
}
