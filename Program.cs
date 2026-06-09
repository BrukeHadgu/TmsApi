var builder = WebApplication.CreateBuilder(args);

//Register the training authentication scheme
builder.Services
    .AddAuthentication("Training")
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions,
    TrainingAuthHandler>("Training", null);

builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();

//Middleware Pipeline
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseExceptionHandler("/error");
app.UseHttpsRedirection();

app.UseRouting(); //Matches URL to endpoint
app.UseAuthentication();
app.UseAuthorization();

//Endpoints
app.MapGet("/api/assessments/results", () => Results.Ok(new
{
    courseCode  = "CS-101",
    studentId   = "S-001",
    letterGrade = "A"
}))
.RequireAuthorization();

app.Run();