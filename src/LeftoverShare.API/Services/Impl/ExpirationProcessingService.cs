using System.Text.Json;
using LeftoverShare.API.Entities;
using LeftoverShare.API.Entities.Enums;
using LeftoverShare.API.Helpers;
using LeftoverShare.API.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LeftoverShare.API.Services.Impl;

/// <summary>
/// 过期清理处理服务实现
/// </summary>
public class ExpirationProcessingService : IExpirationProcessingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly DailyCleanupSettings _settings;
    private readonly ILogger<ExpirationProcessingService> _logger;

    public ExpirationProcessingService(
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        IOptions<DailyCleanupSettings> settings,
        ILogger<ExpirationProcessingService> logger)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// 执行过期清理任务
    /// </summary>
    public async Task<ExpirationProcessingResult> ProcessExpiredItemsAsync()
    {
        var result = new ExpirationProcessingResult();
        var now = DateTime.UtcNow;
        var notificationData = new List<(int UserId, NotificationType Type, string Title, string Content, int? SharePostId, int? ReservationId)>();

        _logger.LogInformation("开始执行过期清理任务，当前时间: {Now}", now);

        try
        {
            var expiredPostIds = await ProcessExpiredSharePostsAsync(now, notificationData, result);
            result.ExpiredSharePostsCount = expiredPostIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理过期分享帖时发生异常");
            result.HasErrors = true;
            result.Errors.Add($"处理过期分享帖异常: {ex.Message}");
        }

        try
        {
            var unusedTimeout = TimeSpan.FromHours(_settings.PickupCodeUnusedTimeoutHours);
            var expiredCodeCount = await ProcessExpiredPickupCodesAsync(now, unusedTimeout, notificationData, result);
            result.ExpiredPickupCodesCount = expiredCodeCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理超时取餐码时发生异常");
            result.HasErrors = true;
            result.Errors.Add($"处理超时取餐码异常: {ex.Message}");
        }

        try
        {
            if (notificationData.Any())
            {
                var sentCount = await _notificationService.SendBulkNotificationsAsync(notificationData);
                result.NotificationsSentCount = sentCount;
                _logger.LogInformation("批量通知发送完成: {SentCount}/{TotalCount}",
                    sentCount, notificationData.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送通知时发生异常");
            result.HasErrors = true;
            result.Errors.Add($"发送通知异常: {ex.Message}");
        }

        result.Details["ProcessedAt"] = now.ToString("o");
        result.Details["ExpiredSharePosts"] = result.ExpiredSharePostsCount;
        result.Details["ExpiredPickupCodes"] = result.ExpiredPickupCodesCount;
        result.Details["NotificationsSent"] = result.NotificationsSentCount;

        _logger.LogInformation(
            "过期清理任务执行完成: 过期分享帖={PostCount}, 超时取餐码={CodeCount}, 发送通知={NotifyCount}, 是否有错误={HasErrors}",
            result.ExpiredSharePostsCount, result.ExpiredPickupCodesCount,
            result.NotificationsSentCount, result.HasErrors);

        return result;
    }

    /// <summary>
    /// 处理过期分享帖
    /// </summary>
    private async Task<int> ProcessExpiredSharePostsAsync(
        DateTime now,
        List<(int UserId, NotificationType Type, string Title, string Content, int? SharePostId, int? ReservationId)> notificationData,
        ExpirationProcessingResult result)
    {
        var expiredPosts = await _unitOfWork.SharePosts.GetExpiredPostsAsync(now);
        var processedCount = 0;
        var postDetails = new List<object>();

        foreach (var post in expiredPosts)
        {
            try
            {
                post.Status = SharePostStatus.Expired;
                post.ExpirationReason = ExpirationReason.PastExpiryTime;
                post.ExpiredAt = now;
                _unitOfWork.SharePosts.Update(post);

                notificationData.Add((
                    UserId: post.PosterId,
                    Type: NotificationType.SharePostExpired,
                    Title: "您的分享帖已过期",
                    Content: $"您发布的分享帖「{post.Title}」已超过可领取时间（{post.AvailableUntil:yyyy-MM-dd HH:mm}），已自动标记为过期状态。",
                    SharePostId: post.Id,
                    ReservationId: null
                ));

                foreach (var reservation in post.Reservations
                             .Where(r => r.Status != ReservationStatus.Completed
                                      && r.Status != ReservationStatus.Cancelled))
                {
                    notificationData.Add((
                        UserId: reservation.ClaimerId,
                        Type: NotificationType.SharePostExpired,
                        Title: "预约的分享帖已过期",
                        Content: $"您预约的分享帖「{post.Title}」已超过可领取时间，该预约已失效。",
                        SharePostId: post.Id,
                        ReservationId: reservation.Id
                    ));
                }

                postDetails.Add(new { PostId = post.Id, Title = post.Title, PosterId = post.PosterId });
                processedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理分享帖过期失败: PostId={PostId}", post.Id);
                result.HasErrors = true;
                result.Errors.Add($"分享帖 PostId={post.Id} 处理失败: {ex.Message}");
            }
        }

        if (processedCount > 0)
        {
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("已处理 {Count} 条过期分享帖", processedCount);
        }

        result.Details["ExpiredPostDetails"] = postDetails;
        return processedCount;
    }

    /// <summary>
    /// 处理超时未使用的取餐码
    /// </summary>
    private async Task<int> ProcessExpiredPickupCodesAsync(
        DateTime now,
        TimeSpan unusedThreshold,
        List<(int UserId, NotificationType Type, string Title, string Content, int? SharePostId, int? ReservationId)> notificationData,
        ExpirationProcessingResult result)
    {
        var expiredCodes = await _unitOfWork.PickupCodes.GetUnusedExpiredCodesAsync(now, unusedThreshold);
        var processedCount = 0;
        var codeDetails = new List<object>();

        foreach (var code in expiredCodes)
        {
            try
            {
                code.IsExpired = true;
                code.ExpirationReason = ExpirationReason.PickupCodeUnusedOver24h;
                code.ExpiredAt = now;
                _unitOfWork.PickupCodes.Update(code);

                var post = code.Reservation?.Post;
                var claimer = code.Reservation?.Claimer;
                var poster = post?.Poster;

                if (poster != null)
                {
                    notificationData.Add((
                        UserId: poster.Id,
                        Type: NotificationType.PickupCodeExpired,
                        Title: "取餐码超时未使用",
                        Content: $"分享帖「{post?.Title}」的取餐码已超过{_settings.PickupCodeUnusedTimeoutHours}小时未被使用，已自动失效。",
                        SharePostId: post?.Id,
                        ReservationId: code.ReservationId
                    ));
                }

                if (claimer != null)
                {
                    notificationData.Add((
                        UserId: claimer.Id,
                        Type: NotificationType.PickupCodeExpired,
                        Title: "您的取餐码已失效",
                        Content: $"您预约的分享帖「{post?.Title}」取餐码（{code.Code}）已超过{_settings.PickupCodeUnusedTimeoutHours}小时未使用，已自动失效。",
                        SharePostId: post?.Id,
                        ReservationId: code.ReservationId
                    ));
                }

                codeDetails.Add(new
                {
                    CodeId = code.Id,
                    Code = code.Code,
                    ReservationId = code.ReservationId,
                    CreatedAt = code.CreatedAt
                });
                processedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理取餐码失效失败: CodeId={CodeId}", code.Id);
                result.HasErrors = true;
                result.Errors.Add($"取餐码 CodeId={code.Id} 处理失败: {ex.Message}");
            }
        }

        if (processedCount > 0)
        {
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("已处理 {Count} 条超时取餐码", processedCount);
        }

        result.Details["ExpiredCodeDetails"] = codeDetails;
        return processedCount;
    }
}
