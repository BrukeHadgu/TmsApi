var builder = WebApplication.CreateBuilder(args);

//Register the training authentication scheme
builder.Services
    .AddAuthentication("Training")
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions,
    TrainingAuthHandler>("Training", null);

//Register authorization services
builder.Services.AddAuthorization();

//Register controllers
builder.Services.AddControllers();
var app = builder.Build();

//Middleware Pipeline
app.UseRouting();           //Matches URL to endpoint
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