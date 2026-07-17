using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs;
using TmsApi.Infrastructure.Services;
using TmsApi.Domain.Entities;
using TmsApi.DTOs;

namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/students")]
[Tags("Students")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class StudentsController(
    IStudentService studentService,
    LinkGenerator linkGenerator) : ControllerBase
{
   /*
   // GET /api/students?page=1&pageSize=10&search=abebe
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<StudentResponseDto>), StatusCodes.Status200OK)]
    [EndpointSummary("List students with pagination")]
    [EndpointDescription("Returns a paginated, optionally filtered list of TMS students. PageSize is capped at 50.")]
    public async Task<IActionResult> GetStudents(
        [FromQuery] PagedRequest request,
        CancellationToken ct)
    {
        var result = await studentService.GetStudentsAsync(request, ct);
        return Ok(result);
    }
    */

    // GET /api/students/{id}
    [HttpGet("{id:int}", Name = nameof(GetStudentById))]
    [ProducesResponseType(typeof(StudentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a student by ID")]
    [EndpointDescription("Returns student details with HATEOAS links. Returns 404 if the student does not exist.")]
    public async Task<IActionResult> GetStudentById(int id, CancellationToken ct)
    {
        var student = await studentService.GetByIdAsync(id, ct);
        if (student is null) return NotFound();

        // Build links using LinkGenerator — never hand-rolled strings
        var selfHref = linkGenerator.GetPathByName(
            HttpContext, nameof(GetStudentById), new { id })!;

        var enrollmentsHref = linkGenerator.GetPathByAction(
            HttpContext,
            action:     "GetStudentEnrollments",
            controller: "Students",
            values:     new { studentId = id })!;

        var links = new List<LinkDto>
        {
            new(selfHref,        "self",        "GET"),
            new(selfHref,        "update",      "PUT"),
            new(selfHref,        "delete",      "DELETE"),
            new(enrollmentsHref, "enrollments", "GET"),
        };

        var detail = new StudentDetailDto
        {
            Id                 = student.Id,
            RegistrationNumber = student.RegistrationNumber,
            Name               = student.Name,
            GPA                = student.GPA,
            IsActive           = student.IsActive,
            EnrollmentCount    = student.EnrollmentCount,
            Links              = links
        };

        return Ok(detail);
    }

    // GET /api/students/{studentId}/enrollments
    [HttpGet("{studentId:int}/enrollments", Name = "GetStudentEnrollments")]
    [ProducesResponseType(typeof(IReadOnlyList<EnrollmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("List enrollments for a student")]
    [EndpointDescription("Returns all enrollments for the specified student.")]
    public async Task<IActionResult> GetStudentEnrollments(int studentId, CancellationToken ct)
    {
        var student = await studentService.GetByIdAsync(studentId, ct);
        if (student is null) return NotFound();

        var enrollments = await studentService.GetEnrollmentsByStudentAsync(studentId, ct);
        return Ok(enrollments);
    }

    // POST /api/students
    [HttpPost]
    [ProducesResponseType(typeof(StudentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Create a new student")]
    [EndpointDescription("Creates a student with a unique registration number. Returns 409 if the registration number already exists.")]
    public async Task<IActionResult> CreateStudent(
        CreateStudentRequest request,
        CancellationToken ct)
    {
        // Business rule — 409 if registration number already exists
        if (await studentService.RegistrationNumberExistsAsync(request.RegistrationNumber, ct))
        {
            return Conflict(new ProblemDetails
            {
                Title  = "Registration number already exists",
                Detail = $"A student with registration number '{request.RegistrationNumber}' is already registered.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var result = await studentService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetStudentById), new { id = result.Id }, result);
    }
}