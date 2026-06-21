using LeftoverShare.API.Entities;
using LeftoverShare.API.Entities.Enums;
using LeftoverShare.API.Helpers;
using LeftoverShare.API.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LeftoverShare.API.Services.Impl;

/// <summary>
/// 过期清理处理服务实现
/// 过期状态变更与站内通知保存使用同一工作单元事务提交，保证数据一致性
/// </summary>
public class ExpirationProcessingService : IExpirationProcessingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly DailyCleanupSettings _settings;
    private readonly ILogger<ExpirationProcessingService> _logger;

    public ExpirationProcessingService(
        IUnitOfWork unitOfWork,
        IOptions<DailyCleanupSettings> settings,
        ILogger<ExpirationProcessingService> logger)
    {
        _unitOfWork = unitOfWork;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// 执行过期清理任务
    /// 所有变更（分享帖状态、取餐码状态、通知记录）将在同一事务中提交
    /// </summary>
    public async Task<ExpirationProcessingResult> ProcessExpiredItemsAsync()
    {
        var result = new ExpirationProcessingResult();
        var now = DateTime.UtcNow;
        var pendingNotifications = new List<Notification>();

        _logger.LogInformation("开始执行过期清理任务（统一事务模式），当前时间: {Now}", now);

        try
        {
            var expiredPosts = await ProcessExpiredSharePostsAsync(now, pendingNotifications, result);
            result.ExpiredSharePostsCount = expiredPosts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理过期分享帖时发生异常，整体事务将回滚");
            result.HasErrors = true;
            result.Errors.Add($"处理过期分享帖异常: {ex.Message}");
            throw;
        }

        try
        {
            var unusedTimeout = TimeSpan.FromHours(_settings.PickupCodeUnusedTimeoutHours);
            var expiredCodes = await ProcessExpiredPickupCodesAsync(now, unusedTimeout, pendingNotifications, result);
            result.ExpiredPickupCodesCount = expiredCodes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理超时取餐码时发生异常，整体事务将回滚");
            result.HasErrors = true;
            result.Errors.Add($"处理超时取餐码异常: {ex.Message}");
            throw;
        }

        try
        {
            foreach (var notification in pendingNotifications)
            {
                await _unitOfWork.Notifications.AddAsync(notification);
            }

            var changedCount = await _unitOfWork.SaveChangesAsync();
            result.NotificationsSentCount = pendingNotifications.Count;

            _logger.LogInformation(
                "统一事务提交成功: 过期帖={Posts}, 超时码={Codes}, 通知={Notifies}, 影响行数={Changed}",
                result.ExpiredSharePostsCount, result.ExpiredPickupCodesCount,
                result.NotificationsSentCount, changedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "统一事务提交失败，所有变更已回滚");
            result.HasErrors = true;
            result.Errors.Add($"事务提交异常: {ex.Message}");
            throw;
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
    /// 处理过期分享帖（仅修改内存状态，不提交）
    /// </summary>
    private async Task<int> ProcessExpiredSharePostsAsync(
        DateTime now,
        List<Notification> pendingNotifications,
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

                pendingNotifications.Add(new Notification
                {
                    UserId = post.PosterId,
                    Type = NotificationType.SharePostExpired,
                    Title = "您的分享帖已过期",
                    Content = $"您发布的分享帖「{post.Title}」已超过可领取时间（{post.AvailableUntil:yyyy-MM-dd HH:mm}），已自动标记为过期状态。",
                    SharePostId = post.Id,
                    ReservationId = null,
                    CreatedAt = now
                });

                foreach (var reservation in post.Reservations
                             .Where(r => r.Status != ReservationStatus.Completed
                                      && r.Status != ReservationStatus.Cancelled))
                {
                    pendingNotifications.Add(new Notification
                    {
                        UserId = reservation.ClaimerId,
                        Type = NotificationType.SharePostExpired,
                        Title = "预约的分享帖已过期",
                        Content = $"您预约的分享帖「{post.Title}」已超过可领取时间，该预约已失效。",
                        SharePostId = post.Id,
                        ReservationId = reservation.Id,
                        CreatedAt = now
                    });
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

        result.Details["ExpiredPostDetails"] = postDetails;
        _logger.LogDebug("已标记 {Count} 条过期分享帖（待统一提交）", processedCount);
        return processedCount;
    }

    /// <summary>
    /// 处理超时未使用的取餐码（仅修改内存状态，不提交）
    /// </summary>
    private async Task<int> ProcessExpiredPickupCodesAsync(
        DateTime now,
        TimeSpan unusedThreshold,
        List<Notification> pendingNotifications,
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
                    pendingNotifications.Add(new Notification
                    {
                        UserId = poster.Id,
                        Type = NotificationType.PickupCodeExpired,
                        Title = "取餐码超时未使用",
                        Content = $"分享帖「{post?.Title}」的取餐码已超过{_settings.PickupCodeUnusedTimeoutHours}小时未被使用，已自动失效。",
                        SharePostId = post?.Id,
                        ReservationId = code.ReservationId,
                        CreatedAt = now
                    });
                }

                if (claimer != null)
                {
                    pendingNotifications.Add(new Notification
                    {
                        UserId = claimer.Id,
                        Type = NotificationType.PickupCodeExpired,
                        Title = "您的取餐码已失效",
                        Content = $"您预约的分享帖「{post?.Title}」取餐码（{code.Code}）已超过{_settings.PickupCodeUnusedTimeoutHours}小时未使用，已自动失效。",
                        SharePostId = post?.Id,
                        ReservationId = code.ReservationId,
                        CreatedAt = now
                    });
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

        result.Details["ExpiredCodeDetails"] = codeDetails;
        _logger.LogDebug("已标记 {Count} 条超时取餐码（待统一提交）", processedCount);
        return processedCount;
    }
}
