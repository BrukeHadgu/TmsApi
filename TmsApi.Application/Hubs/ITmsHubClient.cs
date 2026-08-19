namespace TmsApi.Application.Hubs;

// Strongly typed hub client interface
// Defines what messages the server can SEND to clients
public interface ITmsHubClient
{
    Task ReceiveTranscriptReady(string reportId, string downloadUrl);
    Task ReceiveCourseUpdate(string courseCode, string message);
    Task ReceiveGradePosted(string courseCode, int studentId, decimal grade);
    Task ReceiveEnrollmentStatusUpdated(string enrollmentId, string status);
}