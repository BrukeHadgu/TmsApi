using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TmsApi.Api.Hubs;
using TmsApi.Application.Hubs;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/enrollments")]
[Tags("EnrollmentsAdmin")]
[Produces("application/json")]
public class EnrollmentsFlatController(
    TmsDbContext context,
    IHubContext<TmsHub, ITmsHubClient> hubContext) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Get all enrollments (admin view)")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var enrollments = await context.Enrollments
            .AsNoTracking()
            .Include(e => e.Student)
            .Include(e => e.Course)
            .Select(e => new
            {
                id = e.Id.ToString(),
                studentId = e.StudentId,
                studentName = e.Student.Name,
                courseId = e.CourseId,
                courseName = e.Course.Title,
                status = e.Status,
                enrolledAt = e.EnrolledAt
            })
            .ToListAsync(ct);

        return Ok(enrollments);
    }

    [HttpPost("{id}/approve")]
    [EndpointSummary("Approve an enrollment")]
    public async Task<IActionResult> Approve(int id, CancellationToken ct)
    {
        var enrollment = await context.Enrollments.FindAsync(id, ct);
        if (enrollment is null)
            return NotFound(new ProblemDetails
            {
                Title = "Enrollment not found",
                Status = StatusCodes.Status404NotFound
            });

        if (enrollment.Status == "Approved")
            return Conflict(new ProblemDetails
            {
                Title = "Already approved",
                Status = StatusCodes.Status409Conflict
            });

        enrollment.Status = "Approved";
        await context.SaveChangesAsync(ct);

        // Broadcast to ALL connected Angular clients via SignalR
        await hubContext.Clients.All
            .ReceiveEnrollmentStatusUpdated(id.ToString(), "Approved");

        return Ok(new { id = enrollment.Id, status = enrollment.Status });
    }

    [HttpPost("{id}/reject")]
    [EndpointSummary("Reject an enrollment")]
    public async Task<IActionResult> Reject(int id, CancellationToken ct)
    {
        var enrollment = await context.Enrollments.FindAsync(id, ct);
        if (enrollment is null)
            return NotFound(new ProblemDetails
            {
                Title = "Enrollment not found",
                Status = StatusCodes.Status404NotFound
            });

        if (enrollment.Status == "Rejected")
            return Conflict(new ProblemDetails
            {
                Title = "Already rejected",
                Status = StatusCodes.Status409Conflict
            });

        enrollment.Status = "Rejected";
        await context.SaveChangesAsync(ct);

        // Broadcast to ALL connected Angular clients via SignalR
        await hubContext.Clients.All
            .ReceiveEnrollmentStatusUpdated(id.ToString(), "Rejected");

        return Ok(new { id = enrollment.Id, status = enrollment.Status });
    }
}