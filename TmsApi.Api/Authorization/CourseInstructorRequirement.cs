using Microsoft.AspNetCore.Authorization;

namespace TmsApi.Api.Authorization;
// no property injection in requirements, so we need to use a handler to check if the user is an instructor for the course
public class CourseInstructorRequirement : IAuthorizationRequirement { }