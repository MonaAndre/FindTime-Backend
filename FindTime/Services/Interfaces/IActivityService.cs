using FindTime.Common;
using FindTime.DTOs.ActivityDTOs;
using FindTime.Models;

namespace FindTime.Services.Interfaces;

public interface IActivityService
{
    // Fire-and-forget style, same as INotificationService.NotifyAsync: never
    // throws, so a logging hiccup never fails the action that triggered it.
    Task LogActivityAsync(
        ActivityType type,
        string actorUserId,
        int groupId,
        string groupName,
        int? eventId,
        string? eventName);

    Task<ServiceResponse<List<ActivityDtoResponse>>> GetGroupActivityAsync(
        int groupId,
        string userId,
        int page,
        int pageSize);

    // Cross-group feed: activity from every group the user is an active
    // member of, not just one. Includes the user's own actions too - same
    // "across all my groups" behavior as GetAllEventsNextWeekAsync.
    Task<ServiceResponse<List<ActivityDtoResponse>>> GetUserActivityAsync(
        string userId,
        int page,
        int pageSize);
}
