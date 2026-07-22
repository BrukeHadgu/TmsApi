using TmsApi.Domain.Entities;
namespace TmsApi.Application.Interfaces;
public interface ICourseServices
{
    Task<Course?> GetByCodeAsync(string code, CancellationToken ct);
}