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

        [HttpGet("n-plus-one")]
        public async Task<IActionResult> NPlusOne(CancellationToken cancellationToken)
        {
            // query 1 loads all students one sql statement
            var students = await context.Students
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var results = new List<object>();

            foreach (var s in students)
            {
                // query 2,3,4,5,6.. one SQL statement per student n sql statements
                var count = await context.Enrollments
                    .AsNoTracking()
                    .CountAsync(e => e.StudentId == s.Id, cancellationToken);

                results.Add(new { s.Name, EnrollmentCount = count });
                Console.WriteLine($"{s.Name}: {count} enrollments");
            }

            return Ok(results);
        }

        [HttpGet("n-plus-one-fixed")]
        public async Task<IActionResult> NPlusOneFixed(CancellationToken cancellationToken)
        {
            // one sql statement
            var report = await context.Students
                .AsNoTracking()
                .Select(s => new
                {
                    s.Name,
                    EnrollmentCount = s.Enrollments.Count
                })
                .ToListAsync(cancellationToken);

            foreach (var r in report)
                Console.WriteLine($"{r.Name}: {r.EnrollmentCount} enrollments");

            return Ok(report);
        }

        [HttpGet("n-plus-one-include")]
        public async Task<IActionResult> NPlusOneInclude(CancellationToken cancellationToken)
        {
            var students = await context.Students
                .AsNoTracking()
                .Include(s => s.Enrollments)  // loads all enrollments in one query
                .ToListAsync(cancellationToken);

            foreach (var s in students)
                Console.WriteLine($"{s.Name}: {s.Enrollments.Count} enrollments");

            return Ok(students.Select(s => new
            {
                s.Name,
                EnrollmentCount = s.Enrollments.Count
            }));
        }

        [HttpPut("students/{id}/name")]
        public async Task<IActionResult> UpdateStudentName(
            int id, 
            [FromBody] string newName,
            CancellationToken cancellationToken)
        {
            var student = await context.Students.FindAsync(id, cancellationToken);
            if (student is null) return NotFound();

            student.Name = newName;

            // set the shadow property invisible on the c# object but saves to db
            context.Entry(student).Property("LastUpdated").CurrentValue = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            return Ok(student);
        }

        [HttpGet("students/{id}/load")]
        public async Task<IActionResult> LoadStudent(int id, CancellationToken cancellationToken)
        {
            var student = await context.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
            return student is null ? NotFound() : Ok(student);
        }

        [HttpPut("students/{id}/concurrent-update")]
        public async Task<IActionResult> ConcurrentUpdate(
            int id,
            [FromBody] UpdateStudentRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var student = await context.Students
                    .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

                if (student is null) return NotFound();

                // Simulate delay — another request could update in this gap
                await Task.Delay(5000, cancellationToken);

                student.Name = request.Name;
                student.GPA = request.GPA;

                await context.SaveChangesAsync(cancellationToken);
                return Ok(student);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return Conflict(new
                {
                    Message = "Another user updated this student. Please reload and try again.",
                    Detail = ex.Message
                });
            }
        }
            public record UpdateStudentRequest(string Name, decimal GPA);


        // soft-deleted students automatically hidden
        [HttpGet("students/active")]
        public async Task<IActionResult> GetActiveStudents(CancellationToken cancellationToken)
        {
            var students = await context.Students
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return Ok(students);
        }

        // soft delete a student
        [HttpDelete("students/{id}/soft")]
        public async Task<IActionResult> SoftDeleteStudent(
            int id,
            CancellationToken cancellationToken)
        {
            var student = await context.Students.FindAsync(id, cancellationToken);
            if (student is null) return NotFound();

            student.IsDeleted = true;
            await context.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        // admin endpoint sees all students including soft deleted
        [HttpGet("students/admin/all")]
        public async Task<IActionResult> GetAllStudentsAdmin(CancellationToken cancellationToken)
        {
            var students = await context.Students
                .AsNoTracking()
                .IgnoreQueryFilters()
                .ToListAsync(cancellationToken);

            return Ok(students);
        }

        // admin restore undelete a student
        [HttpPost("students/{id}/restore")]
        public async Task<IActionResult> RestoreStudent(
            int id,
            CancellationToken cancellationToken)
        {
            var student = await context.Students
                .IgnoreQueryFilters()  // must bypass filter to find deleted students
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

            if (student is null) return NotFound();

            student.IsDeleted = false;
            await context.SaveChangesAsync(cancellationToken);

            return Ok(student);
        }



        [HttpPost("enrollments/archive")]
        public async Task<IActionResult> BulkArchiveOldEnrollments(
            CancellationToken cancellationToken)
        {
            var cutoffDate = DateTime.UtcNow.AddYears(-1);
            var archivedCount = await context.Enrollments
                .Where(e => e.EnrolledAt < cutoffDate && !e.IsArchived)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(e => e.IsArchived, true),
                    cancellationToken);

            return Ok(new
            {
                Message = $"Archived {archivedCount} enrollments",
                CutoffDate = cutoffDate
            });
        }
        [HttpGet("enrollments/archived-count")]
        public async Task<IActionResult> GetArchivedCount(CancellationToken cancellationToken)
        {
            var count = await context.Enrollments
                .IgnoreQueryFilters()
                .CountAsync(e => e.IsArchived, cancellationToken);

            return Ok(new { ArchivedEnrollments = count });
        }
}





/*
# Exercise 7 — N+1 vs fixed
curl http://localhost:5001/api/test/n-plus-one
curl http://localhost:5001/api/test/n-plus-one-fixed

# Exercise 8 — shadow property
curl http://localhost:5001/api/test/students/active

# Exercise 9 — soft delete
curl -X DELETE http://localhost:5001/api/test/students/1/soft
curl http://localhost:5001/api/test/students/active
curl http://localhost:5001/api/test/students/admin/all

# Exercise 9 — bulk archive
curl -X POST http://localhost:5001/api/test/enrollments/archive
curl http://localhost:5001/api/test/enrollments/archived-count

*/