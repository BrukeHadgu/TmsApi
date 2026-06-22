using Microsoft.AspNetCore.Mvc;
[ApiController]
[Route("api/enrollments")]
public class EnrollmentsController(IEnrollmentService enrollmentService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var enrollments = await enrollmentService.GetAllAsync();
        return Ok(enrollments); // 200 OK
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var record = await enrollmentService.GetByIdAsync(id);
        return record is not null ? Ok(record) : NotFound(); // 200 or 404
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEnrollmentRequest request)
    {
        var record = await enrollmentService.EnrollAsync(request.StudentId, request.CourseCode);
        return CreatedAtAction(nameof(GetById), new { id = record.Id }, record);//201 plus location
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await enrollmentService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound(); // 204 or 404
    }
}
public record CreateEnrollmentRequest(string StudentId, string CourseCode);

































/*  
# 1. GET all
Invoke-WebRequest -Uri "http://localhost:5000/api/enrollments" -Method GET

# 2. POST new enrollment
Invoke-WebRequest -Uri "http://localhost:5000/api/enrollments" -Method POST -ContentType "application/json" -Body '{"studentId":"S-001","courseCode":"CS-101"}'

# 3. GET by ID (replace {id} with actual ID from step 2)
Invoke-WebRequest -Uri "http://localhost:5000/api/enrollments/abc123" -Method GET

# 4. DELETE
Invoke-WebRequest -Uri "http://localhost:5000/api/enrollments/abc123" -Method DELETE

# 5. GET deleted (should be 404)
Invoke-WebRequest -Uri "http://localhost:5000/api/enrollments/abc123" -Method GET  */



