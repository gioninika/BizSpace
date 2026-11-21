using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BizSpace.Controllers;

[Authorize]
public sealed class NotificationsHub : Hub
{
}
