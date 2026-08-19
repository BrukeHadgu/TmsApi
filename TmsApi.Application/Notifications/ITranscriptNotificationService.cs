namespace TmsApi.Application.Notifications;

// Application layer defines the interface
// Infrastructure/Api layer implements it
// Worker calls this without knowing SignalR exists
public interface ITranscriptNotificationService
{
    Task NotifyTranscriptReadyAsync(int studentId, string reportId, string downloadUrl);
}