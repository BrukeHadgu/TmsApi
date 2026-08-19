using Microsoft.AspNetCore.SignalR;
using TmsApi.Application.Hubs;
using TmsApi.Api.Hubs;
using TmsApi.Application.Notifications;

namespace TmsApi.Api.Notifications;

// This class lives in Api layer (where TmsHub is visible)
// Worker only sees ITranscriptNotificationService — never knows SignalR exists
public class SignalRTranscriptNotificationService(
    IHubContext<TmsHub, ITmsHubClient> hubContext)
    : ITranscriptNotificationService
{
    public async Task NotifyTranscriptReadyAsync(
        int studentId,
        string reportId,
        string downloadUrl)
    {
        // Send ONLY to the specific student's group — never broadcast to all
        await hubContext.Clients
            .Group(GroupNames.Student(studentId.ToString()))
            .ReceiveTranscriptReady(reportId, downloadUrl);
    }
}