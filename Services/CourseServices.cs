public interface ICourseService
{
    Task<CourseRecord> CreateAsync(string title, int capacity);
    Task<CourseRecord?> GetByIdAsync(string code);
    Task<IReadOnlyList<CourseRecord>> GetAllAsync();
    Task<bool> DeleteAsync(string code);
}
public class CourseService : ICourseService
{
    private readonly Dictionary<string, CourseRecord> _store = new();
    private readonly ILogger<CourseService> _logger;
    public CourseService(ILogger<CourseService> logger)
    {
        _logger = logger;
    }

    public Task<CourseRecord> CreateAsync(string title, int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(capacity), "Capacity must be greater than zero.");

        var code = Guid.NewGuid().ToString("N")[..8];
        var record = new CourseRecord(code, title, capacity, 0);
        _store[code] = record;
        _logger.LogInformation(
            "Created course {CourseCode} title {Title}", code, title);
        return Task.FromResult(record);
    }

    public Task<CourseRecord?> GetByIdAsync(string code)
    {
        _store.TryGetValue(code, out var record);
        if (record is null)
            _logger.LogWarning("Course {CourseCode} not found", code);
        return Task.FromResult(record);
    }

    public Task<IReadOnlyList<CourseRecord>> GetAllAsync()
    {
        IReadOnlyList<CourseRecord> all = _store.Values.ToList();
        return Task.FromResult(all);
    }

    public Task<bool> DeleteAsync(string code)
    {
        var removed = _store.Remove(code);
        if (removed)
            _logger.LogInformation("Deleted course {CourseCode}", code);
        else
            _logger.LogWarning(
                "Delete failed course {CourseCode} not found", code);
        return Task.FromResult(removed);
    }
}