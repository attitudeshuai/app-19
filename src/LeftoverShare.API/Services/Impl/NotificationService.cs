using LeftoverShare.API.Entities;
using LeftoverShare.API.Entities.Enums;
using LeftoverShare.API.Repositories;
using Microsoft.Extensions.Logging;

namespace LeftoverShare.API.Services.Impl;

/// <summary>
/// 站内通知服务实现
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IUnitOfWork unitOfWork, ILogger<NotificationService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// 发送单条通知
    /// </summary>
    public async Task<bool> SendNotificationAsync(
        int userId,
        NotificationType type,
        string title,
        string content,
        int? sharePostId = null,
        int? reservationId = null)
    {
        try
        {
            var notification = new Notification
            {
                UserId = userId,
                Type = type,
                Title = title,
                Content = content,
                SharePostId = sharePostId,
                ReservationId = reservationId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Notifications.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("通知发送成功: UserId={UserId}, Type={Type}, Title={Title}",
                userId, type, title);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "通知发送失败: UserId={UserId}, Type={Type}", userId, type);
            return false;
        }
    }

    /// <summary>
    /// 批量发送通知
    /// </summary>
    public async Task<int> SendBulkNotificationsAsync(
        IEnumerable<(int UserId, NotificationType Type, string Title, string Content, int? SharePostId, int? ReservationId)> notifications)
    {
        var successCount = 0;
        var notificationList = new List<Notification>();

        foreach (var item in notifications)
        {
            try
            {
                notificationList.Add(new Notification
                {
                    UserId = item.UserId,
                    Type = item.Type,
                    Title = item.Title,
                    Content = item.Content,
                    SharePostId = item.SharePostId,
                    ReservationId = item.ReservationId,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "构建通知数据失败: UserId={UserId}, Type={Type}",
                    item.UserId, item.Type);
            }
        }

        if (notificationList.Any())
        {
            try
            {
                foreach (var n in notificationList)
                {
                    await _unitOfWork.Notifications.AddAsync(n);
                }
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("批量通知保存成功: {Count} 条", notificationList.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量通知保存失败");
                successCount = 0;
            }
        }

        return successCount;
    }
}
