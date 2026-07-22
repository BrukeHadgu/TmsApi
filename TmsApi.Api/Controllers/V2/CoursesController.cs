using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.Interfaces;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/courses")]
[ApiVersion("2.0")]
public class CoursesController(ICachedCourseService cachedCourseService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCourses(
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct     = default)
    {
        page     = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var allCourses = await cachedCourseService.GetAllCoursesAsync(ct);

        var totalCount = allCourses.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var hasNext    = page < totalPages;
        var hasPrev    = page > 1;

        var rows = allCourses
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(new
        {
            data = rows,
            meta = new { totalCount, page, pageSize, totalPages, hasNext, hasPrevious = hasPrev },
            links = new
            {
                self   = $"/api/v2/courses?page={page}&pageSize={pageSize}",
                next   = hasNext ? $"/api/v2/courses?page={page + 1}&pageSize={pageSize}" : null,
                prev   = hasPrev ? $"/api/v2/courses?page={page - 1}&pageSize={pageSize}" : null,
                enroll = "/api/v2/enrollments"
            }
        });
    }
}