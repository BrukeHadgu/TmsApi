using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Services;
public class CourseQueryService(TmsDbContext context) : ICourseServices
{
    public Task<Course?> GetByCodeAsync(string code, CancellationToken ct) =>
        context.Courses
            .AsNoTracking()
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(c => c.Code == code, ct);

    public Task<CourseResponseDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
    public Task<CourseResponseDto> CreateAsync(CreateCourseRequest request, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
    public Task<bool> CodeExistsAsync(string code, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
    public Task<PagedResponse<CourseResponseDto>> GetCoursesAsync(PagedRequest request, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}