using Microsoft.AspNetCore.SignalR;
using TmsApi.Application.Hubs;
namespace TmsApi.Api.Hubs;

public class TmsHub : Hub<ITmsHubClient>
{
    public override async Task OnConnectedAsync()
    {
        // Auto-join student to their personal group on connect
        // Production: replace with Context.UserIdentifier from JWT (M12)
        var studentId = Context.GetHttpContext()?.Request.Query["studentId"].ToString();

        if (!string.IsNullOrWhiteSpace(studentId))
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                GroupNames.Student(studentId));
        }

        await base.OnConnectedAsync();
    }

    // Clients can explicitly join a course group to receive course updates
    public async Task JoinCourseGroup(string courseCode)
    {
        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            GroupNames.Course(courseCode));
    }

    public async Task LeaveCourseGroup(string courseCode)
    {
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            GroupNames.Course(courseCode));
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // SignalR removes connection from all groups automatically on disconnect
        await base.OnDisconnectedAsync(exception);
    }
}

// Centralised group names — prevents typos causing silent failures in production
public static class GroupNames
{
    public static string Student(string studentId) => $"student-{studentId}";
    public static string Course(string courseCode) => $"course-{courseCode}";
}