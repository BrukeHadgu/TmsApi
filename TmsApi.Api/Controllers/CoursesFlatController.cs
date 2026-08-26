using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.DTOs;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Infrastructure.Services;

namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/v1/coursesflat")]
[Produces("application/json")]
public class CoursesFlatController(
    ICourseDbService courseService,
    TmsDbContext context,
    IAuthorizationService authorizationService) : ControllerBase
{
    // GET: api/v1/coursesflat
    [HttpGet]
    [EndpointSummary("Get all courses")]
    public async Task<IActionResult> GetCourses(CancellationToken ct)
    {
        var courses = await courseService.GetAllAsync(ct);

        var response = new
        {
            items = courses,
            totalCount = courses.Count(),
            page = 1,
            pageSize = courses.Count()
        };

        return Ok(response);
    }

    // GET: api/v1/coursesflat/{id}
    [HttpGet("{id:int}")]
    [EndpointSummary("Get a courses with id")]
    public async Task<IActionResult> GetCourseById(int id, CancellationToken ct)
    {
        var course = await courseService.GetByIdAsync(id, ct);

        if (course is null)
            return NotFound();

        return Ok(course);
    }

    // POST: api/v1/coursesflat
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("Create a Course")]
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

    // PUT: api/v1/coursesflat/{id}
    // Requires Instructor or Admin role
    // Resource-based check ensures instructors can only edit their own courses
    [Authorize(Roles = "Instructor,Admin")]
    [HttpPut("{id:int}")]
    [EndpointSummary("Update a course of their own a&i")]
    public async Task<IActionResult> UpdateCourse(
        int id,
        [FromBody] UpdateCourseDto dto,
        CancellationToken ct)
    {
        var course = await context.Courses.FindAsync([id], ct);
        if (course is null) return NotFound();

        // Resource-based authorization check
        // Passes the Course entity to CourseInstructorHandler
        // Handler checks: isAdmin → allow all | isInstructor → check ownership
        var authResult = await authorizationService
            .AuthorizeAsync(User, course, "CanEditCourse");

        if (!authResult.Succeeded)
            return Forbid(); // 403 — authenticated but not authorized for this resource

        course.Title = dto.Title;
        await context.SaveChangesAsync(ct);

        return NoContent(); // 204 — updated successfully
    }

    // DELETE: api/v1/coursesflat/{id}
    // Only Admins can delete courses
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    [EndpointSummary("Delete Courses /ADMIN")]
    public async Task<IActionResult> DeleteCourse(int id, CancellationToken ct)
    {
        var course = await context.Courses.FindAsync([id], ct);
        if (course is null) return NotFound();

        context.Courses.Remove(course);
        await context.SaveChangesAsync(ct);

        return NoContent(); // 204 — deleted successfully
    }
}