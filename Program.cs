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
builder.Services.AddProblemDetails();


builder.Services.AddSingleton<IEnrollmentService, EnrollmentService>();
builder.Services.AddSingleton<EnrollmentWorker>();

builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration("Payments")
    .ValidateDataAnnotations()
    .ValidateOnStart();

var app = builder.Build();

//Middleware Pipeline
app.UseMiddleware<RequestLoggingMiddleware>(); 
app.UseExceptionHandler("/error");

app.UseStatusCodePages();

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/api/error", () =>
{
throw new TmsDatabaseException("Simulated database failure for ProblemDetails testing");
});

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















