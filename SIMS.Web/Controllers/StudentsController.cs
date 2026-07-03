using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIMS.Application;
using SIMS.Domain;
using SIMS.Web.Models;

namespace SIMS.Web.Controllers;

// Student use cases: View Profile, Register Course, Check Grades.
// Low-privilege actor; the [Authorize] role gate is the RBAC «include».
[Authorize(Roles = nameof(UserRole.Student))]
public class StudentsController : Controller
{
    private readonly StudentService _studentService;
    private readonly CourseService _courseService;
    private readonly EnrollmentService _enrollmentService;
    private readonly ReportService _reportService;
    private readonly SimsFacade _facade;
    private readonly Transcript _transcript;

    public StudentsController(
        StudentService studentService,
        CourseService courseService,
        EnrollmentService enrollmentService,
        ReportService reportService,
        SimsFacade facade,
        Transcript transcript)
    {
        _studentService = studentService;
        _courseService = courseService;
        _enrollmentService = enrollmentService;
        _reportService = reportService;
        _facade = facade;
        _transcript = transcript;
    }

    public IActionResult Profile()
    {
        var student = _studentService.GetProfile(User.GetUserId());
        if (student is null) return NotFound();
        return View(student);
    }

    [HttpGet]
    public IActionResult RegisterCourse()
    {
        return View(BuildCourseList());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RegisterCourse(int courseId)
    {
        try
        {
            _facade.EnrollStudent(User.GetUserId(), courseId);
            TempData["Message"] = "Enrolled successfully.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(RegisterCourse));
    }

    public IActionResult CheckGrades()
    {
        var studentId = User.GetUserId();
        var rows = _reportService.GenerateStudentReport(studentId);
        var completed = rows.Where(r => r.Grade.HasValue).Select(r => r.Grade!.Value).ToList();

        var model = new StudentGradesViewModel
        {
            Rows = rows,
            HasCompletedGrades = completed.Count > 0,
            Gpa = completed.Count > 0 ? _transcript.CalculateGpa(completed) : 0.0
        };
        return View(model);
    }

    private IReadOnlyList<CourseEnrolmentRow> BuildCourseList()
    {
        var studentId = User.GetUserId();
        var mine = _enrollmentService.GetStudentEnrollments(studentId).Select(e => e.CourseId).ToHashSet();
        return _courseService.GetAll()
            .Select(c => new CourseEnrolmentRow(c, _courseService.CurrentEnrolmentCount(c.Id), mine.Contains(c.Id)))
            .ToList();
    }
}
