using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FindTime.Hubs;

[Authorize]
public class NotificationHub : Hub
{
}
