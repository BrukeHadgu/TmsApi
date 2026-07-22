using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Application.DTOs;
using TmsApi.Domain.Entities;
using TmsApi.Application.Interfaces;

namespace TmsApi.Infrastructure.Services;
public class CourseService(TmsDbContext context, ILogger<CourseService> logger) : ICourseService, ICourseServices
{
    public Task<CourseResponseDto?> GetByIdAsync(int id, CancellationToken ct) =>
        context.Courses
            .AsNoTracking() // used for readonly purposes
            .Where(c => c.Id == id)
            .Select(c => new CourseResponseDto(
                c.Id,
                c.Code,
                c.Title,
                c.MaxCapacity,
                c.Enrollments.Count))
            .FirstOrDefaultAsync(ct);

    public async Task<CourseResponseDto> CreateAsync(CreateCourseRequest request, CancellationToken ct)
    {
        var course = new Course
        {
            Code        = request.Code,
            Title       = request.Title,
            MaxCapacity = request.MaxCapacity
        };

        context.Courses.Add(course);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Created course {CourseId} ({Code})", course.Id, course.Code);

        return (await GetByIdAsync(course.Id, ct))!;
    }

    public Task<bool> CodeExistsAsync(string code, CancellationToken ct) =>
        context.Courses
            .AsNoTracking()
            .AnyAsync(c => c.Code == code, ct);


    public async Task<PagedResponse<CourseResponseDto>> GetCoursesAsync
    (PagedRequest request,CancellationToken ct)
{
    // no tracking, read-only
    IQueryable<Course> query = context.Courses.AsNoTracking();

    // apply search filter if provided
    if (!string.IsNullOrWhiteSpace(request.Search))
    {
        query = query.Where(c =>
            EF.Functions.ILike(c.Title, $"%{request.Search}%") ||
            EF.Functions.ILike(c.Code,  $"%{request.Search}%"));
    }
    // count before paging
    var totalCount = await query.CountAsync(ct);

    // safe whitelist OrderBy
    query = request.OrderBy switch
    {
        "Code"        => request.Descending
             ? query.OrderByDescending(c => c.Code)
             : query.OrderBy(c => c.Code),
        "MaxCapacity" => request.Descending
             ? query.OrderByDescending(c => c.MaxCapacity)
             : query.OrderBy(c => c.MaxCapacity),
        _             => request.Descending
             ? query.OrderByDescending(c => c.Title)
             : query.OrderBy(c => c.Title)
    };

    //  skip previous pages
    //  take only this page and project to DTO
    var items = await query
        .Skip((request.Page - 1) * request.PageSize)
        .Take(request.PageSize)
        .Select(c => new CourseResponseDto(
            c.Id,
            c.Code,
            c.Title,
            c.MaxCapacity,
            c.Enrollments.Count))
        .ToListAsync(ct);

    return new PagedResponse<CourseResponseDto>
    {
        Items      = items,
        TotalCount = totalCount,
        Page       = request.Page,
        PageSize   = request.PageSize
    };

}
public Task<Course?> GetByCodeAsync(string code, CancellationToken ct) =>
    context.Courses
        .AsNoTracking()
        .Include(c => c.Enrollments)
        .FirstOrDefaultAsync(c => c.Code == code, ct);
}