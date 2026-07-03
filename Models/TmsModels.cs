public record StudentRecord(
    string Id,
    string Name,
    int Age,
    decimal GPA
);

public record CreateStudentRequest(
    string Name,
    int Age,
    decimal GPA
);
public class TmsDatabaseException(string message) : Exception(message);