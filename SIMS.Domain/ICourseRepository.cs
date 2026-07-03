namespace SIMS.Domain;

public interface ICourseRepository
{
    Course? GetById(int id);
    IEnumerable<Course> GetAll();
    void Add(Course course);
    void Update(Course course);
    void Delete(int id);
}
