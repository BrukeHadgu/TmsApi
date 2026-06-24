public interface IStudentService
{
    Task<StudentRecord> CreateAsync(string name, int age, decimal gpa);
    Task<StudentRecord?> GetByIdAsync(string id);
    Task<IReadOnlyList<StudentRecord>> GetAllAsync();
    Task<bool> DeleteAsync(string id);
}

public class StudentService : IStudentService
{
    private readonly Dictionary<string, StudentRecord> _store = new();
    private readonly ILogger<StudentService> _logger;

    public StudentService(ILogger<StudentService> logger)
    {
        _logger = logger;
    }

    public Task<StudentRecord> CreateAsync(string name, int age, decimal gpa)
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var record = new StudentRecord(id, name, age, gpa);
        _store[id] = record;
        _logger.LogInformation(
            "Created student {StudentId} name {Name}", id, name);
        return Task.FromResult(record);
    }

    public Task<StudentRecord?> GetByIdAsync(string id)
    {
        _store.TryGetValue(id, out var record);
        if (record is null)
            _logger.LogWarning("Student {StudentId} not found", id);
        return Task.FromResult(record);
    }

    public Task<IReadOnlyList<StudentRecord>> GetAllAsync()
    {
        IReadOnlyList<StudentRecord> all = _store.Values.ToList();
        return Task.FromResult(all);
    }

    public Task<bool> DeleteAsync(string id)
    {
        var removed = _store.Remove(id);
        if (removed)
            _logger.LogInformation("Deleted student {StudentId}", id);
        else
            _logger.LogWarning("Delete failed student {StudentId} not found", id);
        return Task.FromResult(removed);
    }


    


}