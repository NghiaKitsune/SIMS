using SIMS.Domain;

namespace SIMS.Application;

// Course catalogue management plus the capacity rule. The capacity check lives
// here (not in the controller or the facade) so that if the rule changes, only
// this class changes.
public class CourseService
{
    private readonly ICourseRepository _courseRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;

    public CourseService(ICourseRepository courseRepository, IEnrollmentRepository enrollmentRepository)
    {
        _courseRepository = courseRepository;
        _enrollmentRepository = enrollmentRepository;
    }

    public Course Create(string code, string title, int credits, int capacity)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Course code is required.", nameof(code));
        if (credits <= 0)
            throw new ArgumentException("Credits must be positive.", nameof(credits));
        if (capacity <= 0)
            throw new ArgumentException("Capacity must be positive.", nameof(capacity));

        var course = new Course { Code = code, Title = title, Credits = credits, Capacity = capacity };
        _courseRepository.Add(course);
        return course;
    }

    public void Update(Course course) => _courseRepository.Update(course);

    public void Delete(int id) => _courseRepository.Delete(id);

    public Course? GetById(int id) => _courseRepository.GetById(id);

    public IEnumerable<Course> GetAll() => _courseRepository.GetAll();

    public int CurrentEnrolmentCount(int courseId) =>
        _enrollmentRepository.GetByCourse(courseId).Count(e => e.Status == EnrollmentStatus.Active);

    public bool HasAvailableSeats(int courseId)
    {
        var course = _courseRepository.GetById(courseId)
            ?? throw new InvalidOperationException($"Course {courseId} not found.");
        return CurrentEnrolmentCount(courseId) < course.Capacity;
    }
}
