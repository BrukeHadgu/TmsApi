using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.DTOs;
using TmsApi.Infrastructure.Services;
using TmsApi.Application.DTOs;

namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/studentsflat")]
[Produces("application/json")]
public class StudentsFlatController(
    IStudentService studentService) : ControllerBase
{

    // GET: api/students-flat
    [HttpGet]
    public async Task<IActionResult> GetStudents(CancellationToken ct)
    {
        var request = new PagedRequest
        {
            Page = 1,
            PageSize = 100
        };

        var result = await studentService.GetStudentsAsync(request, ct);

        return Ok(result.Items);
    }


    // GET: api/students-flat/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetStudentById(
        int id,
        CancellationToken ct)
    {
        var student = await studentService.GetByIdAsync(id, ct);

        if (student is null)
            return NotFound();


        return Ok(new
        {
            student.Id,
            student.RegistrationNumber,
            student.Name,
            student.GPA,
            student.IsActive,
            student.EnrollmentCount
        });
    }


    // POST: api/students-flat
    [HttpPost]
    public async Task<IActionResult> CreateStudent(
        CreateStudentRequest request,
        CancellationToken ct)
    {
        if (await studentService.RegistrationNumberExistsAsync(
            request.RegistrationNumber, ct))
        {
            return Conflict(new
            {
                message = "Registration number already exists"
            });
        }


        var result = await studentService.CreateAsync(request, ct);

        return Ok(result);
    }
}