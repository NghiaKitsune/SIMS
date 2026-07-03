using System.ComponentModel.DataAnnotations;
using SIMS.Application;
using SIMS.Domain;

namespace SIMS.Web.Models;

public class LoginViewModel
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}

public class RegisterViewModel
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Student ID")]
    public string StudentId { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Academic Year")]
    public string AcademicYear { get; set; } = string.Empty;
}

public class CourseFormModel
{
    public int Id { get; set; }

    [Required]
    public string Code { get; set; } = string.Empty;

    [Required]
    public string Title { get; set; } = string.Empty;

    [Range(1, 20)]
    public int Credits { get; set; } = 3;

    [Range(1, 500)]
    public int Capacity { get; set; } = 30;
}

// Row shown to a student on "Register Course": the course plus whether they are
// already enrolled and whether seats remain.
public record CourseEnrolmentRow(Course Course, int Enrolled, bool AlreadyEnrolled);

public class StudentGradesViewModel
{
    public IReadOnlyList<StudentGradeRow> Rows { get; init; } = [];
    public double Gpa { get; init; }
    public bool HasCompletedGrades { get; init; }
}

// One enrolment line for a faculty member: the enrolment joined with the student.
public record CourseResultRow(Enrollment Enrollment, Student? Student);

public class FacultyCourseViewModel
{
    public Course? SelectedCourse { get; init; }
    public IReadOnlyList<Course> Courses { get; init; } = [];
    public IReadOnlyList<CourseResultRow> Results { get; init; } = [];
}

public class ManageAccountsViewModel
{
    public IReadOnlyList<Faculty> Faculty { get; init; } = [];
    public IReadOnlyList<Administrator> Administrators { get; init; } = [];
}

public class CreateFacultyModel
{
    [Required] public string Username { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
    [Required][Display(Name = "Employee ID")] public string EmployeeId { get; set; } = string.Empty;
    [Required] public string Department { get; set; } = string.Empty;
}

public class CreateAdministratorModel
{
    [Required] public string Username { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
    [Display(Name = "Admin Level")] public string AdminLevel { get; set; } = "Standard";
}
