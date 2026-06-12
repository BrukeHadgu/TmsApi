//interface what the system can do
//controller and worker depend on this interface, not the concrete class
public interface IEnrollmentService
{Task<EnrollmentRecord> EnrollAsync(string studentId, string courseCode);
Task<EnrollmentRecord?> GetByIdAsync(string id);
Task<IReadOnlyList<EnrollmentRecord>> GetAllAsync();
Task<bool> DeleteAsync(string id);
}
//The in-memory implementation ---
// stores enrollments in a Dictionary (RAM)
public class EnrollmentService : IEnrollmentService
{
private readonly Dictionary<string, EnrollmentRecord> _store = new();
private readonly ILogger<EnrollmentService> _logger;
public EnrollmentService(ILogger<EnrollmentService> logger)


{
_logger = logger;
}
public Task<EnrollmentRecord> EnrollAsync(string studentId, string courseCode)
{
var id = Guid.NewGuid().ToString("N")[..8];
var record = new EnrollmentRecord(id, studentId, courseCode, DateTime.UtcNow);
_store[id] = record;
_logger.LogInformation(
"Enrolled {StudentId} in {CourseCode} record {EnrollmentId}",
studentId, courseCode, id);
return Task.FromResult(record);
}
public Task<EnrollmentRecord?> GetByIdAsync(string id)
{
_store.TryGetValue(id, out var record);
return Task.FromResult(record);
}
public Task<IReadOnlyList<EnrollmentRecord>> GetAllAsync()
{
IReadOnlyList<EnrollmentRecord> all = _store.Values.ToList();
return Task.FromResult(all);
}
public Task<bool> DeleteAsync(string id)
{
var removed = _store.Remove(id);
return Task.FromResult(removed);
}
}
//The data shape
public record EnrollmentRecord(
string Id,
string StudentId,
string CourseCode,
DateTime EnrolledAt);


































/*// ── The contract (interface) This defines WHAT the service can do — not HOW it does it
// The controller and worker depend on this interface, not the concrete class
// This means you can swap implementations (in-memory → database) without
// changing any code that uses the service
public interface IEnrollmentService
{
    Task<EnrollmentRecord> EnrollAsync(string studentId, string courseCode);
    Task<EnrollmentRecord?> GetByIdAsync(string id);
    Task<IReadOnlyList<EnrollmentRecord>> GetAllAsync();
    Task<bool> DeleteAsync(string id);
}

// ── The in-memory implementation ──────────────────────────────────────
// This stores enrollments in a Dictionary (RAM only)
// In M5 this becomes a real database call with EF Core
public class EnrollmentService : IEnrollmentService
{
    // Dictionary acts as our in-memory database
    // Key = enrollment ID, Value = the enrollment record
    private readonly Dictionary<string, EnrollmentRecord> _store = new();

    // Logger injected by the framework — used for structured logging
    private readonly ILogger<EnrollmentService> _logger;

    public EnrollmentService(ILogger<EnrollmentService> logger)
    {
        _logger = logger;
    }

    public Task<EnrollmentRecord> EnrollAsync(string studentId, string courseCode)
    {
        // ── Duplicate check ───────────────────────────────────────────
        // Before creating a new enrollment, check if this student
        // is already enrolled in this course
        var existing = _store.Values
            .FirstOrDefault(e => e.StudentId == studentId
                              && e.CourseCode == courseCode);

        if (existing is not null)
        {
            // LogWarning — unexpected but recoverable
            // {StudentId} and {CourseCode} are queryable properties
            _logger.LogWarning(
                "Duplicate enrollment attempt {StudentId} already in {CourseCode} (record {EnrollmentId})",
                studentId, courseCode, existing.Id);

            return Task.FromResult(existing);
        }

        // Generate a short unique ID for this enrollment
        // Same pattern as M1 Session 3
        var id = Guid.NewGuid().ToString("N")[..8];

        var record = new EnrollmentRecord(id, studentId, courseCode, DateTime.UtcNow);

        // Store it in our in-memory dictionary
        _store[id] = record;

        // LogInformation — business event completed successfully
        _logger.LogInformation(
            "Enrolled {StudentId} in {CourseCode} record {EnrollmentId}",
            studentId, courseCode, id);

        // Task.FromResult wraps a synchronous value in a Task
        // so the method signature matches async (real DB calls in M5)
        return Task.FromResult(record);
    }

    public Task<EnrollmentRecord?> GetByIdAsync(string id)
    {
        _store.TryGetValue(id, out var record);

        if (record is null)
        {
            // LogWarning — record not found, unexpected but not broken
            _logger.LogWarning("Enrollment {EnrollmentId} not found", id);
        }

        return Task.FromResult(record);
    }

    public Task<IReadOnlyList<EnrollmentRecord>> GetAllAsync()
    {
        IReadOnlyList<EnrollmentRecord> all = _store.Values.ToList();
        return Task.FromResult(all);
    }

    public Task<bool> DeleteAsync(string id)
    {
        var removed = _store.Remove(id);

        if (removed)
            _logger.LogInformation("Deleted enrollment {EnrollmentId}", id);
        else
            _logger.LogWarning("Delete failed enrollment {EnrollmentId} not found", id);

        return Task.FromResult(removed);
    }
}

// ── The data shape ────────────────────────────────────────────────────
// record = immutable by default (from M1)
// Once created, the values cannot be changed
public record EnrollmentRecord(
    string Id,
    string StudentId,
    string CourseCode,
    DateTime EnrolledAt);*/