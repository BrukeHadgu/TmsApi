public record StudentRecord(
    string Id,
    string Name,
    int Age,
    decimal GPA
);

public record CourseRecord(
    string Code,
    string Title,
    int Capacity,
    int EnrolledCount
);

public record CreateStudentRequest(
    string Name,
    int Age,
    decimal GPA
);

public record CreateCourseRequest(
    string Title,
    int Capacity
);