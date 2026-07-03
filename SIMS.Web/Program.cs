using Microsoft.AspNetCore.Authentication.Cookies;
using SIMS.Application;
using SIMS.Application.Strategies;
using SIMS.Domain;
using SIMS.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// --- Composition root (DIP): all wiring lives here, the one place that knows
// which concrete storage/hashing/email implementation each abstraction uses. ---

// CSV data lives under App_Data next to the app content root.
var dataDir = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataDir);
string DataPath(string file) => Path.Combine(dataDir, file);

// Repositories are singletons: the file is the shared store and the caching
// decorator's in-memory dictionary must be shared across requests to be useful.
builder.Services.AddSingleton<IStudentRepository>(_ =>
    // Decorator wraps the CSV repository, exactly as Program.cs composes it in the report.
    new CachingStudentRepository(new CsvStudentRepository(DataPath("students.csv"))));
builder.Services.AddSingleton<IFacultyRepository>(_ => new CsvFacultyRepository(DataPath("faculty.csv")));
builder.Services.AddSingleton<IAdministratorRepository>(_ => new CsvAdministratorRepository(DataPath("administrators.csv")));
builder.Services.AddSingleton<ICourseRepository>(_ => new CsvCourseRepository(DataPath("courses.csv")));
builder.Services.AddSingleton<IEnrollmentRepository>(_ => new CsvEnrollmentRepository(DataPath("enrollments.csv")));

builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddSingleton<IEmailService, ConsoleEmailService>();

// Default GPA rule for transcripts; swapping to FourPointGpaStrategy is a
// one-line change here (Strategy + OCP).
builder.Services.AddScoped<IGpaCalculationStrategy, TenPointGpaStrategy>();
builder.Services.AddScoped<Transcript>();

// Application services.
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddScoped<StudentService>();
builder.Services.AddScoped<CourseService>();
builder.Services.AddScoped<EnrollmentService>();
builder.Services.AddScoped<UserManagementService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<SimsFacade>();

// Hand-rolled cookie authentication (no ASP.NET Identity), backed by the CSV
// repositories — matches the Assignment 1 design decision.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Seed once so the app is usable on first run (admin account + sample data).
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    DataSeeder.SeedIfEmpty(
        sp.GetRequiredService<IStudentRepository>(),
        sp.GetRequiredService<IFacultyRepository>(),
        sp.GetRequiredService<IAdministratorRepository>(),
        sp.GetRequiredService<ICourseRepository>(),
        sp.GetRequiredService<IPasswordHasher>());
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

// Exposed so SIMS.Tests can host the real app with WebApplicationFactory<Program>.
public partial class Program { }
