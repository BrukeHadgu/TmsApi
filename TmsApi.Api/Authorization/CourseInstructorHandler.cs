using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using TmsApi.Domain.Entities;

namespace TmsApi.Api.Authorization;

public class CourseInstructorHandler
    : AuthorizationHandler<CourseInstructorRequirement, Course>
{
  protected override Task HandleRequirementAsync(
      AuthorizationHandlerContext context,
      CourseInstructorRequirement requirement,
      Course resource)
  {
    var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
    var isInstructor = context.User.IsInRole("Instructor");
    var isAdmin = context.User.IsInRole("Admin");

    // Admins can manage any course — no ownership check needed
    if (isAdmin)
    {
      context.Succeed(requirement);
      return Task.CompletedTask;
    }

    // Instructors can only manage courses of their own
    // InstructorId must match the caller's user ID from JWT
    if (isInstructor && resource.InstructorId == userId)
    {
      context.Succeed(requirement);
    }
    // If neither condition is met — handler does nothing
    // Authorization fails by default (403 Forbidden)
    return Task.CompletedTask;
  }
}