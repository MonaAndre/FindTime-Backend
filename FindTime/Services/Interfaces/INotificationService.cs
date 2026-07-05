using FindTime.Common;
using FindTime.DTOs.NotificationDTOs;
using FindTime.Models;

namespace FindTime.Services.Interfaces;

public interface INotificationService
{
    Task<ServiceResponse<List<NotificationDtoResponse>>> GetNotificationsAsync(
        string userId, bool unreadOnly, int page, int pageSize);

    Task<ServiceResponse<int>> GetUnreadCountAsync(string userId);

    Task<ServiceResponse<bool>> MarkAsReadAsync(int notificationId, string userId);

    Task<ServiceResponse<bool>> MarkAllAsReadAsync(string userId);

    // Fire-and-forget style: writes the row(s) and pushes over SignalR to
    // whoever's connected. Never throws - failures are logged, not surfaced,
    // so a notification hiccup never fails the action that triggered it.
    Task NotifyAsync(string userId, NotificationType type, string message, int? eventId, int? groupId);

    Task NotifyManyAsync(IEnumerable<string> userIds, NotificationType type, string message, int? eventId,
        int? groupId);
}
