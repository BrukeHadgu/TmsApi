using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.DTOs;
using TmsApi.Infrastructure.Services;

namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/v1/coursesflat")]
[Produces("application/json")]
public class CoursesFlatController(
    ICourseDbService courseService) : ControllerBase
{
    // GET: api/coursesflat
    [HttpGet]
    public async Task<IActionResult> GetCourses(
        CancellationToken ct)
    {
        var courses = await courseService.GetAllAsync(ct);
        
        // ✅ Return a PagedResponse object
        var response = new
        {
            items = courses,
            totalCount = courses.Count(),
            page = 1,
            pageSize = courses.Count()
        };

        return Ok(response);
    }

    // GET: api/coursesflat/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCourseById(
        int id,
        CancellationToken ct)
    {
        var course = await courseService.GetByIdAsync(id, ct);

        if (course is null)
            return NotFound();

        return Ok(course);
    }

    // POST: api/coursesflat
    [HttpPost]
    public async Task<IActionResult> CreateCourse(
        [FromBody] CreateCourseRequest request,
        CancellationToken ct)
    {
        if (await courseService.CodeExistsAsync(request.Code, ct))
        {
            return Conflict(new
            {
                message = "Course code already exists"
            });
        }

        var result = await courseService.CreateAsync(request, ct);
        return Ok(result);
    }
}