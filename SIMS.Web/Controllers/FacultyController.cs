using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIMS.Application;
using SIMS.Domain;
using SIMS.Web.Models;

namespace SIMS.Web.Controllers;

// Faculty use cases: View Student Results, Submit Grades, Generate Report.
// Medium-privilege actor gated by the Faculty role (RBAC «include»).
[Authorize(Roles = nameof(UserRole.Faculty))]
public class FacultyController : Controller
{
    private readonly CourseService _courseService;
    private readonly EnrollmentService _enrollmentService;
    private readonly StudentService _studentService;
    private readonly ReportService _reportService;

    public FacultyController(
        CourseService courseService,
        EnrollmentService enrollmentService,
        StudentService studentService,
        ReportService reportService)
    {
        _courseService = courseService;
        _enrollmentService = enrollmentService;
        _studentService = studentService;
        _reportService = reportService;
    }

    // Dashboard: pick a course to view its student results.
    public IActionResult Index()
    {
        return View(new FacultyCourseViewModel
        {
            Courses = _courseService.GetAll().OrderBy(c => c.Code).ToList()
        });
    }

    // View Student Results for one course (+ inline Submit Grades form per row).
    public IActionResult Course(int id)
    {
        var course = _courseService.GetById(id);
        if (course is null) return NotFound();

        var results = _enrollmentService.GetCourseEnrollments(id)
            .Select(e => new CourseResultRow(e, _studentService.GetProfile(e.StudentId)))
            .ToList();

        return View(new FacultyCourseViewModel
        {
            SelectedCourse = course,
            Courses = _courseService.GetAll().OrderBy(c => c.Code).ToList(),
            Results = results
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SubmitGrade(int enrollmentId, double grade, int courseId)
    {
        try
        {
            _enrollmentService.SubmitGrade(enrollmentId, grade);
            TempData["Message"] = "Grade submitted.";
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or InvalidOperationException)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Course), new { id = courseId });
    }

    public IActionResult GenerateReport()
    {
        return View(_reportService.GenerateInstitutionReport());
    }
}
