using BizSpace.Dtos;
using BizSpace.Repositories.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BizSpace.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController(INotificationService notifications) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<NotificationDto>>>> GetMine([FromQuery] bool unreadOnly = false) =>
        Ok(ApiResponse<List<NotificationDto>>.Ok(await notifications.GetMineAsync(CurrentUserId, unreadOnly)));

    [HttpPut("{id:int}/read")]
    public async Task<ActionResult<ApiResponse<NotificationDto>>> MarkRead(int id) =>
        Ok(ApiResponse<NotificationDto>.Ok(await notifications.MarkReadAsync(CurrentUserId, id), "Notification marked read."));

    [HttpPut("read-all")]
    public async Task<ActionResult<ApiResponse<int>>> MarkAllRead() =>
        Ok(ApiResponse<int>.Ok(await notifications.MarkAllReadAsync(CurrentUserId), "Notifications marked read."));
}
