namespace SIMS.Domain;

public enum EnrollmentStatus
{
    Active,
    Completed,
    Withdrawn
}

// Association class resolving the many-to-many Student <-> Course relationship.
// Composition with Course: an Enrollment has no meaning outside the Course it belongs to.
public class Enrollment
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public DateTime EnrolmentDate { get; set; }
    public double? Grade { get; set; }
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
}
