# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

SIMS (Student Information Management System) — the working application built for **Unit 20 Assignment 2** (LO3 build + LO4 automated testing, criteria P5/P6/P7/M3/M4/D2). It is the concrete implementation of the architecture **designed and already submitted in Assignment 1**. The authoritative design source is `../ASMGROUPORIGINALFORALL.docx` (the finished Assignment 1 report by Vo Trong Nghia, BC00616, class SE08101, assessor Mr. Tran Van Nhuom). When code and that report disagree, the report wins — the report is what is being graded against.

`../Assignment_Part_1_SIMS_NhuomTV.docx` (a lecturer-provided guide) and `../../Labs/StudentManagement_Full_Guide.docx` are **reference only**, not the design to build.

## Non-obvious decisions (do not "fix" these)

- **CSV storage, not EF/SQL/Identity.** The Assignment 1 design commits to CSV files via the Repository pattern. `StudentManagement_Full_Guide.docx` uses EF Core + ASP.NET Identity — that is deliberately NOT followed here because it contradicts the submitted design. Do not migrate to EF.
- **Auth is hand-rolled ASP.NET Core cookie auth**, backed by CSV repositories, passwords hashed with PBKDF2 (`Rfc2898DeriveBytes` in `SIMS.Domain/PasswordHashing.cs`). No Identity package.
- **Repository interfaces are split per role** (`IStudentRepository`, `IFacultyRepository`, `IAdministratorRepository`, plus `ICourseRepository`, `IEnrollmentRepository`) — this is intentional ISP, matching the report. Do not merge them into one `IUserRepository`.
- **CSV repositories skip malformed rows** rather than throwing, and use `File.ReadLines()` (deferred/streamed) per the report's large-dataset rationale — keep both properties; integration tests assert them.

## Design patterns wired into the code (named in the report, keep the names)

- **Decorator** — `CachingStudentRepository` wraps any `IStudentRepository`, caches in a `Dictionary<int,Student>`.
- **Factory Method** — `RepositoryFactory` chooses the storage implementation.
- **Strategy** — `IGpaCalculationStrategy` with `FourPointGpaStrategy` / `TenPointGpaStrategy`, injected into `Transcript`.
- **Facade** — `SimsFacade.EnrollStudent(studentId, courseId)` coordinates services; it must stay a pure coordinator (no business logic of its own — the report explicitly warns against it becoming a God Object).
- **Repository** — the `Csv*Repository` classes themselves.

## Layout & dependency rule

Five projects, dependencies point inward only: `SIMS.Web` → `SIMS.Application` → `SIMS.Domain`; `SIMS.Infrastructure` → `SIMS.Domain`; `SIMS.Domain` references nothing; `SIMS.Tests` → all. Never add a reference that makes `SIMS.Domain` depend on another project.

## Commands

```bash
dotnet build SIMS.slnx                                    # 0 errors expected
dotnet test SIMS.slnx                                     # xUnit + Moq; unit + integration
dotnet test SIMS.slnx --collect:"XPlat Code Coverage"     # coverage (report targets >=70% App+Domain)
dotnet run --project SIMS.Web                             # run the app
dotnet test SIMS.slnx --filter FullyQualifiedName~StudentServiceTests  # run a single test class
```

Solution file is `SIMS.slnx` (the newer XML solution format), not `.sln`.

Test convention follows the course (`../../Labs/Lab5/.../PaymentServiceTests.cs`): xUnit `[Fact]`/`[Theory]`+`InlineData`, Moq `Mock<T>` / `mock.Verify(...)`.

## Git

This directory (`ASM/SIMS/`) is its own git repo (not nested inside `btec-skills`), pushed to **https://github.com/NghiaKitsune/SIMS** (private), branch `main`. `.gitignore` excludes `bin/`, `obj/`, `TestResults/`, and `SIMS.Web/App_Data/` (runtime CSV data — reseeds via `DataSeeder` on next run, never commit it). Initial commit `8d52bfd` covers Setup+M1+M2+M3 (147 files). Commit M4 separately when tests land.

## Seed accounts (created on first run by `DataSeeder`)

- Administrator — `admin` / `Admin@123`
- Faculty — `jsmith` / `Faculty@123`
- Student — `nvana` / `Student@123`

## Build progress (living log — update after each milestone)

- ✅ **Setup** — `SIMS.sln` + 5 projects created, references wired, Moq + Microsoft.AspNetCore.Mvc.Testing added to `SIMS.Tests`.
- ✅ **M1 — Domain + Infrastructure** (builds clean, 0 errors). Domain: `User`/`Student`/`Faculty`/`Administrator`, `Course`, `Enrollment`, the 5 split repo interfaces, `IPasswordHasher`, `IGpaCalculationStrategy`, `PasswordHashing` (PBKDF2). Infrastructure: `Csv{Student,Faculty,Administrator,Course,Enrollment}Repository`, `CachingStudentRepository` (Decorator), `RepositoryFactory` (Factory), `Pbkdf2PasswordHasher`, `DataSeeder`, `CsvUtils`/`CsvFile` helpers.
- ✅ **M2 — Application** (builds clean, 0 errors). `AuthenticationService` (LSP via `User.Login`), `StudentService` (`Register` + duplicate guard + email), `CourseService` (capacity rule), `EnrollmentService` (enrol + `SubmitGrade`), `UserManagementService` (admin creates Faculty/Admin), `ReportService` (+ report record DTOs), `SimsFacade` (coordinator only), `Transcript` + `Strategies/{FourPoint,TenPoint}GpaStrategy`, `IEmailService`/`ConsoleEmailService`.
- ✅ **M3 — Web** (builds clean, 0 errors; verified end-to-end at runtime). `Program.cs` is the composition root: singleton CSV repos (`IStudentRepository` = `new CachingStudentRepository(new CsvStudentRepository(App_Data/students.csv))`), scoped services, `IGpaCalculationStrategy` → `TenPointGpaStrategy`, hand-rolled cookie auth (`LoginPath=/Account/Login`, `AccessDeniedPath=/Account/AccessDenied`), `DataSeeder.SeedIfEmpty` on startup, `public partial class Program` for test hosting. Controllers map 1:1 to the Actor-to-Use-Case table: `AccountController` (Login/Logout/Register/AccessDenied, `[AllowAnonymous]`), `StudentsController` `[Authorize(Student)]` (Profile/RegisterCourse via `SimsFacade`/CheckGrades with GPA via `Transcript`), `CoursesController` (Index `[Authorize]`, Create/Edit/Delete `[Authorize(Administrator)]`), `FacultyController` `[Authorize(Faculty)]` (Index/Course results/SubmitGrade/GenerateReport), `AdminController` `[Authorize(Administrator)]` (Index/ManageStudents/DeleteStudent/ManageAccounts/CreateFaculty/CreateAdministrator/GenerateReport). View models in `Models/ViewModels.cs`; `ClaimsPrincipalExtensions.GetUserId()` reads the SIMS Id from the `NameIdentifier` claim; `_Layout.cshtml` renders role-aware nav + TempData alerts; shared `_InstitutionReport.cshtml` partial. **Runtime CSV data lives in `SIMS.Web/App_Data/` — gitignore-style runtime state, safe to delete (reseeds on next run).** Note: `AuthenticationService`/`StudentService` are aliased in `AccountController` to disambiguate from `Microsoft.AspNetCore.Authentication.AuthenticationService`. Smoke-verified: anon→login 200, RBAC cross-role gate returns 302→AccessDenied, full flow student self-enrol → faculty grade 8.5 → student sees grade+GPA, all persisted to real CSV.
- ✅ **M4 — Tests** (builds clean; **58 tests, all pass** — 49 unit + 9 integration). Location `SIMS.Tests/` (references all projects + Moq + `Microsoft.AspNetCore.Mvc.Testing`; placeholder `UnitTest1.cs` deleted). Convention = the course's `Labs/Lab5/.../PaymentServiceTests.cs`: xUnit `[Fact]`/`[Theory]`+`InlineData`, Moq `Mock<T>` + `mock.Verify(...)`. Delivered classes (E2E uses `WebApplicationFactory<Program>` + `ConfigureTestServices` from `Microsoft.AspNetCore.TestHost` to redirect the 5 repo singletons at a fresh temp App_Data; `SimsFacadeTests` builds real services over mocked repos because the concrete services have non-virtual methods Moq can't intercept — still proves coordination at the repo/email boundary):
  - `Unit/StudentServiceTests.cs` — **the example the report explicitly commits to**: `Register()` success path (mock `IStudentRepository` + `IPasswordHasher` + `IEmailService`; assert `repo.Add` called once via `Verify`, password is hashed not plaintext, `SendRegistrationConfirmation` fired) **and** the duplicate-student guard (duplicate username → `InvalidOperationException`; duplicate `StudentId` → `InvalidOperationException`; assert `Add` never called). Also the `ArgumentException` guards on empty username/password/studentId.
  - `Unit/AuthenticationServiceTests.cs` — valid login returns the user; wrong password → null; unknown username → null. Uses real `Pbkdf2PasswordHasher` to seed a known hash, or mocks the repos to return a `User` whose `PasswordHash` was produced by `PasswordHashing.Hash`.
  - `Unit/CourseServiceTests.cs` — `Create` validation (blank code / credits ≤ 0 / capacity ≤ 0 throw); `HasAvailableSeats` true below capacity, false at capacity (mock `IEnrollmentRepository.GetByCourse`).
  - `Unit/EnrollmentServiceTests.cs` — duplicate enrolment guard via `Exists`; `SubmitGrade` sets Grade+`Completed`; grade outside 0–10 → `ArgumentOutOfRangeException`.
  - `Unit/SimsFacadeTests.cs` — assert the facade only *coordinates*: on success it calls `EnrollmentService.Enroll` + email; when course full it throws and `Enroll` is never called (proves the rule lives in the service, not the facade). Mock the collaborators.
  - `Unit/GpaStrategyTests.cs` — `[Theory]`/`InlineData` pure-function checks for `FourPointGpaStrategy` (g/10*4 then mean) and `TenPointGpaStrategy` (mean, 2dp), incl. empty → 0.0.
  - `Integration/CsvRepositoryTests.cs` — **real CSV fixture files in a temp dir** (`Path.GetTempPath()` per test, cleaned up): `GetAll` count, `Add` appends, `Update`/`Delete` don't corrupt neighbours, **field containing a comma round-trips** (via `CsvUtils` quoting), empty optional column, and a **malformed row is skipped without throwing** (the two properties CLAUDE.md says integration tests must assert).
  - `Integration/EndToEndFlowTests.cs` — `WebApplicationFactory<Program>` + `HttpClient`, pointing the app at a fresh temp `App_Data`. Reproduce the smoke flow now proven by hand: seed admin login → create course → student self-register → student enrol → faculty submit grade → assert grade persisted in the real CSV. Must pull the `__RequestVerificationToken` from each GET and send the antiforgery cookie (forms use `[ValidateAntiForgeryToken]`).
- ✅ **Verify** (real numbers, dotnet SDK 10.0.300, captured 2026-07-04): `dotnet build SIMS.slnx` → **0 errors, 0 warnings**. `dotnet test SIMS.slnx` → **58 passed / 0 failed** (49 unit + 9 integration). `dotnet test --collect:"XPlat Code Coverage"` line coverage: **SIMS.Domain 100.0% (104/104), SIMS.Application 66.4% (312/470) → App+Domain combined 72.5% (416/574), meets the report's ≥70% target.** (Context, not the graded target: SIMS.Infrastructure 84.7%, SIMS.Web 49.7%.) `dotnet run --project SIMS.Web` already works (verified in M3).

## Phase B — write the Assignment 2 report (after M4, separate plan)

Not started; needs its own plan before executing. Use `academic-orchestrator` (research → outline → draft → critique → rubric-score P5/P6/P7/M3/M4/D2 → visuals → integrity/voice → Harvard citations → docx). Hard constraints: report prose in **academic English**, all chat/checkpoints in **Vietnamese** (see the top-level bilingual rule). Evidence must be the **real** code + real `dotnet test`/coverage output from M4 — no fabricated numbers or screenshots. Cover-sheet identity is already known (Vo Trong Nghia, BC00616, SE08101, assessor Mr. Tran Van Nhuom — from `../ASMGROUPORIGINALFORALL.docx` / `../FRONTSHEET...docx`), so don't re-ask. Assignment 2 brief (`../Assignment part 2.docx`): Calibri 12, 1.5 spacing, Harvard, 2000–2500 words. Activity 1 must survey **all four** designed test levels (Unit/Integration/Acceptance/Performance) even though only Unit+Integration are executed here; Activity 2 (P7) shows the executed evidence. **Screenshots for Activity 2 must be taken by the user** — no browser/screenshot tool in this environment; they run `dotnet run --project SIMS.Web` and log in with the seed accounts above.

## Intentionally NOT built (stated so, not hidden)

The Assignment 1 test plan describes four test levels. This assignment implements **Unit + Integration** (real evidence for P7). **Acceptance (Playwright), Performance (JMeter), and the GitHub Actions pipeline are described in the report but not executed here** — the report's Activity 1 still surveys all four levels ("examine... as designed in the test plan"). If asked to add Playwright/JMeter, that is a scope change to confirm first.
