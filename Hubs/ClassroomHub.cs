using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AIClassroom.Hubs;

[Authorize]
public class ClassroomHub : Hub
{
    private static readonly HashSet<string> AllowedCommands = new(StringComparer.Ordinal) { "previous", "next", "quiz", "game" };

    private bool IsTeacher => Context.User?.IsInRole("SuperAdmin") == true || Context.User?.IsInRole("Teacher") == true;

    // Students may only join the room of their own group + class ("A-4"); teachers may join any room.
    private bool CanUseRoom(string room)
    {
        if (string.IsNullOrWhiteSpace(room) || room.Length > 20) return false;
        if (IsTeacher) return true;
        var group = Context.User?.FindFirst("Group")?.Value;
        var cls = Context.User?.FindFirst("ClassNumber")?.Value;
        return Context.User?.IsInRole("Student") == true && !string.IsNullOrEmpty(group) && !string.IsNullOrEmpty(cls) && room == $"{group}-{cls}";
    }

    public async Task JoinClass(string room)
    {
        if (!CanUseRoom(room)) throw new HubException("You cannot join that classroom.");
        await Groups.AddToGroupAsync(Context.ConnectionId, room);
    }

    public async Task LeaveClass(string room)
    {
        if (!CanUseRoom(room)) throw new HubException("You cannot leave that classroom.");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, room);
    }

    public async Task TeacherCommand(string room, string command)
    {
        if (!IsTeacher) throw new HubException("Only a teacher can control a live classroom.");
        if (!CanUseRoom(room) || !AllowedCommands.Contains(command)) throw new HubException("Invalid command.");
        await Clients.Group(room).SendAsync("TeacherCommand", command);
    }

    public async Task SendAnnouncement(string room, string message)
    {
        if (!IsTeacher) throw new HubException("Only a teacher can send classroom announcements.");
        if (!CanUseRoom(room) || string.IsNullOrWhiteSpace(message) || message.Length > 500) throw new HubException("Invalid announcement.");
        await Clients.Group(room).SendAsync("TeacherAnnouncement", message.Trim());
    }
}
