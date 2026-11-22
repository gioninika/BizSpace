using BizSpace.Dtos;
using BizSpace.Exceptions;
using BizSpace.Models;
using BizSpace.Repositories;
using BizSpace.Controllers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BizSpace.Repositories.Services;

public sealed class NotificationService(
    IGenericRepository<Notification> notifications,
    IGenericRepository<UserAccount> users,
    IGenericRepository<Store> stores,
    IHubContext<NotificationsHub> hub,
    ILogger<NotificationService> logger) : INotificationService
{
    public async Task<List<NotificationDto>> GetMineAsync(int userId, bool unreadOnly)
    {
        var query = notifications.Query.AsNoTracking().Include(x => x.Sender)
            .Where(x => x.RecipientUserId == userId);
        if (unreadOnly) query = query.Where(x => x.ReadAtUtc == null);
        var result = await query.OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id).Take(200).ToListAsync();
        return result.Select(ToDto).ToList();
    }

    public async Task<NotificationDto> MarkReadAsync(int userId, int notificationId)
    {
        var notification = await notifications.Query.Include(x => x.Sender)
            .FirstOrDefaultAsync(x => x.Id == notificationId && x.RecipientUserId == userId)
            ?? throw new KeyNotFoundException("Notification was not found.");
        notification.ReadAtUtc ??= DateTime.UtcNow;
        await notifications.SaveAsync();
        return ToDto(notification);
    }

    public async Task<int> MarkAllReadAsync(int userId)
    {
        var unread = await notifications.Query.Where(x => x.RecipientUserId == userId && x.ReadAtUtc == null)
            .ToListAsync();
        foreach (var notification in unread)
            notification.ReadAtUtc = DateTime.UtcNow;
        await notifications.SaveAsync();
        return unread.Count;
    }

    public async Task NotifyAdminsAsync(
        NotificationType type,
        string title,
        string message,
        int? storeId,
        int? relatedEntityId = null)
    {
        var adminIds = await users.Query.AsNoTracking().Where(x => x.Role == UserRoles.Admin)
            .Select(x => x.Id).ToListAsync();
        await NotifyUsersAsync(adminIds, type, title, message, storeId, relatedEntityId);
    }

    public async Task NotifyStoreManagerAsync(
        int storeId,
        NotificationType type,
        string title,
        string message,
        int? relatedEntityId = null)
    {
        var managerId = await stores.Query.Where(x => x.Id == storeId).Select(x => x.ManagerId).FirstOrDefaultAsync();
        if (managerId.HasValue)
            await NotifyUsersAsync([managerId.Value], type, title, message, storeId, relatedEntityId);
    }

    public async Task NotifyUserAsync(
        int userId,
        NotificationType type,
        string title,
        string message,
        int? relatedEntityId = null) =>
        await NotifyUsersAsync([userId], type, title, message, storeId: null, relatedEntityId);

    public async Task<NotificationDto> SendAdminMessageAsync(int storeId, int adminId, string message)
    {
        var store = await stores.Query.Include(x => x.Manager).FirstOrDefaultAsync(x => x.Id == storeId)
            ?? throw new KeyNotFoundException("Store was not found.");
        var admin = await users.Query.FirstOrDefaultAsync(x => x.Id == adminId && x.Role == UserRoles.Admin)
            ?? throw new ForbiddenException("Only an administrator can send manager messages.");
        if (store.ManagerId is not int managerId)
            throw new BusinessRuleException("This store has no assigned manager.");

        var notification = new Notification
        {
            RecipientUserId = managerId,
            SenderUserId = adminId,
            Sender = admin,
            StoreId = storeId,
            Type = NotificationType.AdminMessage,
            Title = $"Message from Admin about {store.Name}",
            Message = message.Trim()
        };
        await notifications.AddAsync(notification);
        await notifications.SaveAsync();
        var dto = ToDto(notification);
        await PushAsync(notification.RecipientUserId, dto);
        return dto;
    }

    private async Task NotifyUsersAsync(
        IEnumerable<int> recipientIds,
        NotificationType type,
        string title,
        string message,
        int? storeId,
        int? relatedEntityId)
    {
        var recipients = recipientIds.Distinct().ToArray();
        if (recipients.Length == 0) return;

        var created = recipients.Select(recipientId => new Notification
        {
            RecipientUserId = recipientId,
            StoreId = storeId,
            RelatedEntityId = relatedEntityId,
            Type = type,
            Title = title,
            Message = message
        }).ToList();
        foreach (var notification in created)
            await notifications.AddAsync(notification);
        await notifications.SaveAsync();

        foreach (var notification in created)
            await PushAsync(notification.RecipientUserId, ToDto(notification));
    }

    private async Task PushAsync(int userId, NotificationDto notification)
    {
        try
        {
            await hub.Clients.User(userId.ToString()).SendAsync("notificationReceived", notification);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not push notification {NotificationId} to user {UserId}.",
                notification.Id, userId);
        }
    }

    private static NotificationDto ToDto(Notification notification) => new()
    {
        Id = notification.Id,
        Type = notification.Type,
        Title = notification.Title,
        Message = notification.Message,
        StoreId = notification.StoreId,
        RelatedEntityId = notification.RelatedEntityId,
        SenderUserId = notification.SenderUserId,
        SenderName = notification.Sender?.DisplayName,
        CreatedAtUtc = notification.CreatedAtUtc,
        ReadAtUtc = notification.ReadAtUtc
    };
}

