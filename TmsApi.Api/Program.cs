using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using TmsApi.Api;
using TmsApi.Api.Extensions;
using TmsApi.Api.Hubs;
using TmsApi.Api.Middlewares;
using TmsApi.Api.Workers;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Identity;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Service Provider Validation ───────────────────────────────────────
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

// ── Register All Services via Extension Methods ───────────────────────
builder.Services.AddSignalR();
builder.Services.AddTmsAuthentication(builder.Configuration);
builder.Services.AddTmsCors(builder.Configuration);
builder.Services.AddTmsControllers();
builder.Services.AddTmsApiVersioning();
builder.Services.AddTmsRateLimiting();
builder.Services.AddTmsMediator();
builder.Services.AddTmsServices();
builder.Services.AddTmsCaching();
builder.Services.AddTmsDatabase(builder.Configuration);
builder.Services.AddTmsOpenApi();
builder.Services.AddTmsErrorHandling();
builder.Services.AddTmsOptions();

var app = builder.Build();

// ── Middleware Pipeline ───────────────────────────────────────────────
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi("openapi/{documentName}.json");
    app.MapScalarApiReference(options =>
    {
        options.Title = "TMS API Reference";
        options
            .AddDocument("v1", "TMS API V1", "/openapi/v1.json")
            .AddDocument("v2", "TMS API V2", "/openapi/v2.json");
    });
}

app.UseRouting();
app.UseCors("TmsClient");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// XSRF cookie middleware — exclude SignalR hub routes
app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/hubs") &&
        (context.User.Identity?.IsAuthenticated == true ||
         context.Request.Cookies.ContainsKey("tms_auth")))
    {
        var antiforgery = context.RequestServices
            .GetRequiredService<IAntiforgery>();

        var tokens = antiforgery.GetAndStoreTokens(context);

        context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!,
            new CookieOptions
            {
                HttpOnly = false,
                Secure = !builder.Environment.IsDevelopment(),
                SameSite = SameSiteMode.Strict
            });
    }

    await next(context);
});

app.UseMiddleware<V1DeprecationMiddleware>();

// ── Endpoints ─────────────────────────────────────────────────────────
app.MapGet("/api/assessments/results", () => Results.Ok(new
{
    courseCode = "CS-101",
    studentId = "S-001",
    letterGrade = "A"
}))
.RequireAuthorization();

app.MapGet("/api/enrollments/worker-smoke", (EnrollmentWorker worker) =>
{
    worker.ProcessBatch();
    return Results.Ok("processed");
});

app.MapGet("/api/error", () =>
{
    throw new TmsDatabaseException("Simulated database failure for ProblemDetails testing");
});

app.MapHub<TmsHub>("/hubs/tms").RequireCors("TmsClient");
app.MapControllers();

// ── Database Seeding ──────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
    context.Database.Migrate();

    if (!context.Students.Any())
    {
        var students = new List<Student>
        {
            new() { RegistrationNumber = "TMS-2026-0001", Name = "Alice Smith",   GPA = 3.8m, IsActive = true  },
            new() { RegistrationNumber = "TMS-2026-0002", Name = "Bob Jones",     GPA = 2.9m, IsActive = true  },
            new() { RegistrationNumber = "TMS-2026-0003", Name = "Charlie Brown", GPA = 3.4m, IsActive = false },
            new() { RegistrationNumber = "TMS-2026-0004", Name = "Diana Prince",  GPA = 3.9m, IsActive = true  },
            new() { RegistrationNumber = "TMS-2026-0005", Name = "Evan Wright",   GPA = 2.5m, IsActive = true  }
        };

        var courses = new List<Course>
        {
            new() { Code = "CS-101",  Title = "Introduction to Computer Science", MaxCapacity = 30 },
            new() { Code = "CS-201",  Title = "Data Structures and Algorithms",   MaxCapacity = 25 },
            new() { Code = "MAT-101", Title = "Calculus I",                       MaxCapacity = 40 }
        };

        context.Courses.AddRange(courses);
        context.SaveChanges();

        var enrollments = new List<Enrollment>
        {
            new() { StudentId = students[0].Id, CourseId = courses[0].Id, Grade = 4.0m },
            new() { StudentId = students[0].Id, CourseId = courses[1].Id, Grade = 3.6m },
            new() { StudentId = students[1].Id, CourseId = courses[0].Id, Grade = 2.8m },
            new() { StudentId = students[3].Id, CourseId = courses[1].Id, Grade = 3.9m }
        };

        context.Enrollments.AddRange(enrollments);
        context.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
    await DataSeeder.SeedAsync(context);
}

app.Run();