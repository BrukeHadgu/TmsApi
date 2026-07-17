using System.ComponentModel.DataAnnotations;
namespace TmsApi.Application.DTOs;

public record CreateStudentRequest
{
    [Required]
    [RegularExpression(@"^TMS-\d{4}-\d{4}$",
        ErrorMessage = "RegistrationNumber must follow the pattern TMS-YYYY-NNNN (e.g. TMS-2026-0001).")]
    public required string RegistrationNumber { get; init; }

    [Required, MaxLength(200)]
    public required string Name { get; init; }

    [Range(0.0, 4.0, ErrorMessage = "GPA must be between 0.0 and 4.0.")]
    public decimal GPA { get; init; }
}