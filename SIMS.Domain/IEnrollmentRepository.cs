namespace SIMS.Domain;

public interface IEnrollmentRepository
{
    Enrollment? GetById(int id);
    IEnumerable<Enrollment> GetByStudent(int studentId);
    IEnumerable<Enrollment> GetByCourse(int courseId);
    IEnumerable<Enrollment> GetAll();
    void Add(Enrollment enrollment);
    void Update(Enrollment enrollment);
    bool Exists(int studentId, int courseId);
}
