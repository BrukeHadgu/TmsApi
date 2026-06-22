namespace TmsApi.Entities;

public class Assessment
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public decimal MaxScore { get; set; }
    public decimal Weight { get; set; } // e.g. 0.30m means this is worth 30% of the final grade

    // Foreign key + navigation to the owning course
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
}