using FindTime.Common;
using FindTime.Data;
using FindTime.DTOs.NotificationDTOs;
using FindTime.Hubs;
using FindTime.Models;
using FindTime.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FindTime.Services.Implementations;

public class NotificationService(ApplicationDbContext context, IHubContext<NotificationHub> hubContext)
    : INotificationService
{
    public async Task<ServiceResponse<List<NotificationDtoResponse>>> GetNotificationsAsync(
        string userId, bool unreadOnly, int page, int pageSize)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize is < 1 or > 100) pageSize = 20;

            var notifications = await context.Notifications
                .Where(n => n.UserId == userId && (!unreadOnly || !n.IsRead))
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NotificationDtoResponse
                {
                    NotificationId = n.NotificationId,
                    Type = n.Type,
                    Message = n.Message,
                    EventId = n.EventId,
                    GroupId = n.GroupId,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    ReadAt = n.ReadAt
                })
                .ToListAsync();

            return ServiceResponse<List<NotificationDtoResponse>>.SuccessResponse(notifications);
        }
        catch (Exception e)
        {
            return ServiceResponse<List<NotificationDtoResponse>>.ErrorResponse(
                $"Failed to get notifications: {e.Message}", 500);
        }
    }

    public async Task<ServiceResponse<int>> GetUnreadCountAsync(string userId)
    {
        try
        {
            var count = await context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            return ServiceResponse<int>.SuccessResponse(count);
        }
        catch (Exception e)
        {
            return ServiceResponse<int>.ErrorResponse($"Failed to get unread count: {e.Message}", 500);
        }
    }

    public async Task<ServiceResponse<bool>> MarkAsReadAsync(int notificationId, string userId)
    {
        try
        {
            var notification = await context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);

            if (notification == null)
            {
                return ServiceResponse<bool>.NotFoundResponse("Notification not found");
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }

            return ServiceResponse<bool>.SuccessResponse(true);
        }
        catch (Exception e)
        {
            return ServiceResponse<bool>.ErrorResponse($"Failed to mark notification as read: {e.Message}", 500);
        }
    }

    public async Task<ServiceResponse<bool>> MarkAllAsReadAsync(string userId)
    {
        try
        {
            var now = DateTime.UtcNow;
            await context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadAt, now));

            return ServiceResponse<bool>.SuccessResponse(true);
        }
        catch (Exception e)
        {
            return ServiceResponse<bool>.ErrorResponse($"Failed to mark notifications as read: {e.Message}", 500);
        }
    }

    public async Task NotifyAsync(string userId, NotificationType type, string message, int? eventId,
        int? groupId)
    {
        await NotifyManyAsync([userId], type, message, eventId, groupId);
    }

    public async Task NotifyManyAsync(IEnumerable<string> userIds, NotificationType type, string message,
        int? eventId, int? groupId)
    {
        try
        {
            var distinctUserIds = userIds.Distinct().ToList();
            if (distinctUserIds.Count == 0)
            {
                return;
            }

            var now = DateTime.UtcNow;
            var notifications = distinctUserIds.Select(id => new Notification
            {
                UserId = id,
                EventId = eventId,
                GroupId = groupId,
                Type = type,
                Message = message,
                CreatedAt = now
            }).ToList();

            context.Notifications.AddRange(notifications);
            await context.SaveChangesAsync();

            foreach (var notification in notifications)
            {
                var dto = new NotificationDtoResponse
                {
                    NotificationId = notification.NotificationId,
                    Type = notification.Type,
                    Message = notification.Message,
                    EventId = notification.EventId,
                    GroupId = notification.GroupId,
                    IsRead = notification.IsRead,
                    CreatedAt = notification.CreatedAt,
                    ReadAt = notification.ReadAt
                };

                await hubContext.Clients.User(notification.UserId).SendAsync("ReceiveNotification", dto);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to create/push notifications: {e.Message}");
        }
    }
}
