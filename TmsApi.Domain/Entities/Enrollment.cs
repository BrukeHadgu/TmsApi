using System;
namespace TmsApi.Domain.Entities;
public class Enrollment
{
    public int Id { get; set; }
    // foreign keys pointing to Students.Id and Courses.Id 
    public int StudentId { get; set; }
    public int CourseId { get; set; } 
    public decimal? Grade { get; set; }//nullable  student may still be enrolled, no grade yet
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public bool IsArchived { get; set; } = false;
    public Student Student { get; set; } = null!;
    public Course Course { get; set; } = null!;

    public string Status { get; set; } = "Pending";
}