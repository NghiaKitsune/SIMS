using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIMS.Application;
using SIMS.Domain;
using SIMS.Web.Models;

namespace SIMS.Web.Controllers;

// Course catalogue. Index is readable by any authenticated actor; management
// (Create/Edit/Delete) is Administrator-only per the Actor-to-Use-Case table.
[Authorize]
public class CoursesController : Controller
{
    private readonly CourseService _courseService;

    public CoursesController(CourseService courseService)
    {
        _courseService = courseService;
    }

    public IActionResult Index()
    {
        return View(_courseService.GetAll().OrderBy(c => c.Code).ToList());
    }

    [Authorize(Roles = nameof(UserRole.Administrator))]
    [HttpGet]
    public IActionResult Create() => View(new CourseFormModel());

    [Authorize(Roles = nameof(UserRole.Administrator))]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(CourseFormModel model)
    {
        if (!ModelState.IsValid) return View(model);
        try
        {
            _courseService.Create(model.Code, model.Title, model.Credits, model.Capacity);
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
        TempData["Message"] = $"Course {model.Code} created.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = nameof(UserRole.Administrator))]
    [HttpGet]
    public IActionResult Edit(int id)
    {
        var course = _courseService.GetById(id);
        if (course is null) return NotFound();
        return View(new CourseFormModel
        {
            Id = course.Id,
            Code = course.Code,
            Title = course.Title,
            Credits = course.Credits,
            Capacity = course.Capacity
        });
    }

    [Authorize(Roles = nameof(UserRole.Administrator))]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(CourseFormModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var course = _courseService.GetById(model.Id);
        if (course is null) return NotFound();

        course.Code = model.Code;
        course.Title = model.Title;
        course.Credits = model.Credits;
        course.Capacity = model.Capacity;
        _courseService.Update(course);

        TempData["Message"] = $"Course {model.Code} updated.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = nameof(UserRole.Administrator))]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        _courseService.Delete(id);
        TempData["Message"] = "Course deleted.";
        return RedirectToAction(nameof(Index));
    }
}
