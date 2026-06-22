using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;

namespace TmsApi.Controllers;
[ApiController]
[Route("api/test")]
public class TestController(TmsDbContext context) : ControllerBase
{
    [HttpGet("deferred")]
    public IActionResult TestDeferred()
    {
        Console.WriteLine("\n>>> STEP 1: Building the query object (no database contact)...");
        var query = context.Students.Where(s => s.GPA >= 3.0m);

        Console.WriteLine(">>> STEP 2: Appending a sorting clause...");
        var orderedQuery = query.OrderBy(s => s.Name);

        Console.WriteLine(">>> STEP 3: Materializing query into a C# List...");
        var results = orderedQuery.ToList(); // SQL runs HERE

        Console.WriteLine(">>> STEP 4: Materialization finished. List populated.\n");
        return Ok(results);
    }

    [HttpGet("students/page/{page}")]
        public async Task<IActionResult> GetStudentsPaged(int page, CancellationToken cancellationToken)
        {
            const int pageSize = 20;
            var students = await context.Students
                .OrderBy(s => s.Name)    //must sort before paging
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return Ok(students);
        }

        [HttpGet("courses/top5")]
        public async Task<IActionResult> GetTopCourses(CancellationToken cancellationToken)
        {
            var topCourses = await context.Courses
                .Select(c => new
                {
                    c.Title,
                    EnrollmentCount = c.Enrollments.Count
                })
                .OrderByDescending(c => c.EnrollmentCount)
                .Take(5)
                .ToListAsync(cancellationToken);

            return Ok(topCourses);
        }


}