using System;
namespace TmsApi.Entities;
public class Enrollment
{
    public int Id { get; set; }
    // foreign keys pointing to Students.Id and Courses.Id 
    public int StudentId { get; set; }
    public int CourseId { get; set; } 
    public decimal? Grade { get; set; }//nullable  student may still be enrolled, no grade yet
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    //Navigation properties let EF Core load the related Student and Course
    public Student Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
}