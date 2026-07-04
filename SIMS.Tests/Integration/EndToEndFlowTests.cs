using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SIMS.Domain;
using SIMS.Infrastructure;

namespace SIMS.Tests.Integration;

// Full-stack test: hosts the real SIMS.Web app with WebApplicationFactory<Program>
// but points its CSV store at a throwaway temp directory. It drives the exact
// smoke flow proven by hand in M3 through real HTTP requests (antiforgery token +
// cookie on every form), then asserts the grade landed in the real CSV file.
public class EndToEndFlowTests : IClassFixture<EndToEndFlowTests.TempDataWebFactory>
{
    private readonly TempDataWebFactory _factory;

    public EndToEndFlowTests(TempDataWebFactory factory) => _factory = factory;

    // WebApplicationFactory that redirects the five repository singletons to a
    // fresh temp App_Data, so DataSeeder and every request read/write there and
    // the developer's real App_Data is never touched.
    public sealed class TempDataWebFactory : WebApplicationFactory<Program>
    {
        public string DataDir { get; } =
            Path.Combine(Path.GetTempPath(), "sims-e2e-" + Guid.NewGuid().ToString("N"));

        private string Data(string file) => Path.Combine(DataDir, file);

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            Directory.CreateDirectory(DataDir);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IStudentRepository>();
                services.RemoveAll<IFacultyRepository>();
                services.RemoveAll<IAdministratorRepository>();
                services.RemoveAll<ICourseRepository>();
                services.RemoveAll<IEnrollmentRepository>();

                // Same composition as Program.cs (caching decorator over CSV).
                services.AddSingleton<IStudentRepository>(_ =>
                    new CachingStudentRepository(new CsvStudentRepository(Data("students.csv"))));
                services.AddSingleton<IFacultyRepository>(_ => new CsvFacultyRepository(Data("faculty.csv")));
                services.AddSingleton<IAdministratorRepository>(_ => new CsvAdministratorRepository(Data("administrators.csv")));
                services.AddSingleton<ICourseRepository>(_ => new CsvCourseRepository(Data("courses.csv")));
                services.AddSingleton<IEnrollmentRepository>(_ => new CsvEnrollmentRepository(Data("enrollments.csv")));
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing && Directory.Exists(DataDir))
            {
                try { Directory.Delete(DataDir, recursive: true); } catch { /* best effort cleanup */ }
            }
        }
    }

    [Fact]
    public async Task FullFlow_AdminCreatesCourse_StudentSelfRegistersAndEnrols_FacultyGrades_PersistsToCsv()
    {
        // --- Admin logs in and creates a course -------------------------------
        var admin = NewClient();
        await LoginAsync(admin, DataSeeder.SeedAdminUsername, DataSeeder.SeedAdminPassword);
        await CreateCourseAsync(admin, code: "E2E101", title: "End To End Testing", credits: 3, capacity: 5);

        var course = new CsvCourseRepository(Path.Combine(_factory.DataDir, "courses.csv"))
            .GetAll().Single(c => c.Code == "E2E101");

        // --- Student self-registers, then logs in and enrols ------------------
        var anon = NewClient();
        await RegisterStudentAsync(anon, username: "teste2e", email: "teste2e@student.sims.edu",
            password: "Student@123", studentId: "BC99999", academicYear: "2025-2026");

        var student = new CsvStudentRepository(Path.Combine(_factory.DataDir, "students.csv"))
            .GetByUsername("teste2e");
        Assert.NotNull(student); // self-registration persisted

        var studentClient = NewClient();
        await LoginAsync(studentClient, "teste2e", "Student@123");
        await EnrolAsync(studentClient, course.Id);

        var enrollment = new CsvEnrollmentRepository(Path.Combine(_factory.DataDir, "enrollments.csv"))
            .GetAll().Single(e => e.StudentId == student!.Id && e.CourseId == course.Id);
        Assert.Equal(EnrollmentStatus.Active, enrollment.Status); // enrolment persisted, not yet graded

        // --- Faculty submits a grade ------------------------------------------
        var faculty = NewClient();
        await LoginAsync(faculty, DataSeeder.SeedFacultyUsername, DataSeeder.SeedFacultyPassword);
        await SubmitGradeAsync(faculty, enrollment.Id, grade: 8.5, courseId: course.Id);

        // --- Assert the grade persisted in the real CSV -----------------------
        var graded = new CsvEnrollmentRepository(Path.Combine(_factory.DataDir, "enrollments.csv"))
            .GetById(enrollment.Id);
        Assert.NotNull(graded);
        Assert.Equal(8.5, graded!.Grade);
        Assert.Equal(EnrollmentStatus.Completed, graded.Status);
    }

    // --- HTTP helpers ---------------------------------------------------------

    private HttpClient NewClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static async Task LoginAsync(HttpClient client, string username, string password)
    {
        var token = await GetTokenAsync(client, "/Account/Login");
        var resp = await client.PostAsync("/Account/Login", Form(new()
        {
            ["Username"] = username,
            ["Password"] = password,
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode); // 302 => authenticated (re-render 200 would mean failure)
    }

    private static async Task RegisterStudentAsync(HttpClient client, string username, string email,
        string password, string studentId, string academicYear)
    {
        var token = await GetTokenAsync(client, "/Account/Register");
        var resp = await client.PostAsync("/Account/Register", Form(new()
        {
            ["Username"] = username,
            ["Email"] = email,
            ["Password"] = password,
            ["StudentId"] = studentId,
            ["AcademicYear"] = academicYear,
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode); // 302 => registration succeeded
    }

    private static async Task CreateCourseAsync(HttpClient client, string code, string title, int credits, int capacity)
    {
        var token = await GetTokenAsync(client, "/Courses/Create");
        var resp = await client.PostAsync("/Courses/Create", Form(new()
        {
            ["Code"] = code,
            ["Title"] = title,
            ["Credits"] = credits.ToString(),
            ["Capacity"] = capacity.ToString(),
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
    }

    private static async Task EnrolAsync(HttpClient client, int courseId)
    {
        var token = await GetTokenAsync(client, "/Students/RegisterCourse");
        var resp = await client.PostAsync("/Students/RegisterCourse", Form(new()
        {
            ["courseId"] = courseId.ToString(),
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
    }

    private static async Task SubmitGradeAsync(HttpClient client, int enrollmentId, double grade, int courseId)
    {
        // The Submit Grades form is rendered on the per-course results page.
        var token = await GetTokenAsync(client, $"/Faculty/Course/{courseId}");
        var resp = await client.PostAsync("/Faculty/SubmitGrade", Form(new()
        {
            ["enrollmentId"] = enrollmentId.ToString(),
            ["grade"] = grade.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["courseId"] = courseId.ToString(),
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
    }

    private static async Task<string> GetTokenAsync(HttpClient client, string url)
    {
        var resp = await client.GetAsync(url);
        resp.EnsureSuccessStatusCode(); // 200 => authorised to see the form
        return ExtractToken(await resp.Content.ReadAsStringAsync());
    }

    private static string ExtractToken(string html)
    {
        var m = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        if (!m.Success)
            m = Regex.Match(html, "value=\"([^\"]+)\"[^>]*name=\"__RequestVerificationToken\"");
        Assert.True(m.Success, "Antiforgery token not found in form HTML.");
        return m.Groups[1].Value;
    }

    private static FormUrlEncodedContent Form(Dictionary<string, string> fields) => new(fields);
}
