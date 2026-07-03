namespace TmsApi.Dtos;
public record CourseResponseDto(
    int Id,
    string Code,
    string Title,
    int MaxCapacity,
    int EnrollmentCount);





































    /*
    PS C:\Users\ed\TmsApi> dotnet build

Restore complete (1.1s)

  TmsApi net10.0 failed with 1 error(s) (7.0s)

    C:\Users\ed\TmsApi\Controllers\EnrollmentsController.cs(36,20): error CS1061: 'Course' does not contain a definition for 'EnrollmentCount' and no accessible extension method 'EnrollmentCount' accepting a first argument of type 'Course' could be found (are you missing a using directive or an assembly reference?)

Build failed with 1 error(s) in 10.1s
    */