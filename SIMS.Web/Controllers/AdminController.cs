using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIMS.Application;
using SIMS.Domain;
using SIMS.Web.Models;

namespace SIMS.Web.Controllers;

// Administrator use cases: Manage Students, Manage Courses (see CoursesController),
// Manage Accounts, Generate Report. High-privilege actor gated by the
// Administrator role (RBAC «include»).
[Authorize(Roles = nameof(UserRole.Administrator))]
public class AdminController : Controller
{
    private readonly StudentService _studentService;
    private readonly UserManagementService _userManagementService;
    private readonly ReportService _reportService;

    public AdminController(
        StudentService studentService,
        UserManagementService userManagementService,
        ReportService reportService)
    {
        _studentService = studentService;
        _userManagementService = userManagementService;
        _reportService = reportService;
    }

    public IActionResult Index() => View(_reportService.GenerateInstitutionReport());

    public IActionResult ManageStudents()
    {
        return View(_studentService.GetAll().OrderBy(s => s.StudentId).ToList());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteStudent(int id)
    {
        _studentService.Delete(id);
        TempData["Message"] = "Student deleted.";
        return RedirectToAction(nameof(ManageStudents));
    }

    [HttpGet]
    public IActionResult ManageAccounts()
    {
        return View(BuildAccountsModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateFaculty(CreateFacultyModel model)
    {
        if (!ModelState.IsValid)
            return View(nameof(ManageAccounts), BuildAccountsModel());
        try
        {
            _userManagementService.CreateFaculty(model.Username, model.Email, model.Password, model.EmployeeId, model.Department);
            TempData["Message"] = $"Faculty '{model.Username}' created.";
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(ManageAccounts));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateAdministrator(CreateAdministratorModel model)
    {
        if (!ModelState.IsValid)
            return View(nameof(ManageAccounts), BuildAccountsModel());
        try
        {
            _userManagementService.CreateAdministrator(model.Username, model.Email, model.Password, model.AdminLevel);
            TempData["Message"] = $"Administrator '{model.Username}' created.";
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(ManageAccounts));
    }

    public IActionResult GenerateReport() => View(_reportService.GenerateInstitutionReport());

    private ManageAccountsViewModel BuildAccountsModel() => new()
    {
        Faculty = _userManagementService.GetAllFaculty().OrderBy(f => f.Username).ToList(),
        Administrators = _userManagementService.GetAllAdministrators().OrderBy(a => a.Username).ToList()
    };
}
