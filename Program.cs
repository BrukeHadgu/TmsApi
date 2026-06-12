using Microsoft.AspNetCore.Authentication;
var builder = WebApplication.CreateBuilder(args);
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes  = true;
    options.ValidateOnBuild = true;
});

//Register the training authentication scheme
builder.Services  
    .AddAuthentication("Training")
    .AddScheme<AuthenticationSchemeOptions, TrainingAuthHandler>("Training", null);

builder.Services.AddAuthorization();
builder.Services.AddControllers();


builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddSingleton<EnrollmentWorker>();

builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration("Payments")
    .ValidateDataAnnotations()
    .ValidateOnStart();

var app = builder.Build();

//Middleware Pipeline
app.UseMiddleware<RequestLoggingMiddleware>(); 
app.UseExceptionHandler("/error");
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/assessments/results", () => Results.Ok(new
{
    courseCode  = "CS-101",
    studentId   = "S-001",
    letterGrade = "A"
}))
.RequireAuthorization(); //after an authentication scheme   

//endpoints  
app.MapGet("/api/enrollments/worker-smoke", (EnrollmentWorker worker) =>
{
    worker.ProcessBatch();
    return Results.Ok("processed");
});
app.Run();



























/*using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// ── Validate DI lifetimes at startup ──────────────────────────────────
// ValidateScopes  — catches captive dependency (scoped inside singleton)
// ValidateOnBuild — runs the check when the app builds, not first request
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes  = true;
    options.ValidateOnBuild = true;
});

// ── Services ──────────────────────────────────────────────────────────

// Authentication — Training scheme from Session 1
builder.Services
    .AddAuthentication("Training")
    .AddScheme<AuthenticationSchemeOptions, TrainingAuthHandler>("Training", null);

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// Exercise 2: DI Lifetimes
// EnrollmentWorker is Singleton — lives for the entire app lifetime
// IEnrollmentService is Scoped — one per request
// EnrollmentWorker uses IServiceScopeFactory to safely resolve scoped services
builder.Services.AddSingleton<EnrollmentWorker>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();

// Exercise 3: Options Pattern
// Bind the "Payments" section of appsettings.json to PaymentOptions
// ValidateDataAnnotations — runs [Required] and [Range] checks
// ValidateOnStart — crashes at startup if invalid, not at runtime
builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration("Payments")
    .ValidateDataAnnotations()
    .ValidateOnStart();

var app = builder.Build();

// ── Middleware Pipeline ────────────────────────────────────────────────
app.UseMiddleware<RequestLoggingMiddleware>(); // outermost — wraps everything
app.UseExceptionHandler("/error");
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ── Endpoints ─────────────────────────────────────────────────────────

// Session 1 endpoint — still protected
app.MapGet("/api/assessments/results", () => Results.Ok(new
{
    courseCode  = "CS-101",
    studentId   = "S-001",
    letterGrade = "A"
}))
.RequireAuthorization();

// Exercise 2 smoke test — tests the worker safely resolves scoped service
app.MapGet("/api/enrollments/worker-smoke", (EnrollmentWorker worker) =>
{
    worker.ProcessBatch();
    return Results.Ok("processed");
});

app.Run();*/



























