using LeftoverShare.API.Data;
using LeftoverShare.API.Entities;
using LeftoverShare.API.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LeftoverShare.API.Services.Impl;

/// <summary>
/// 物理清理服务实现
/// 清理超过保留期的软删除记录（物理删除）
/// 按外键依赖顺序 SharePost > Reservation > PickupCode > KarmaPoint 处理
/// </summary>
public class HardCleanupService : IHardCleanupService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly AppDbContext _context;
    private readonly ILogger<HardCleanupService> _logger;

    public HardCleanupService(
        IUnitOfWork unitOfWork,
        AppDbContext context,
        ILogger<HardCleanupService> logger)
    {
        _unitOfWork = unitOfWork;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 清理指定日期之前的软删除记录
    /// 处理顺序：SharePost → Reservation → PickupCode → KarmaPoint → DeletedEntitySnapshot
    /// </summary>
    public async Task<HardCleanupResult> CleanupExpiredSoftDeletesAsync(DateTime beforeDate)
    {
        var result = new HardCleanupResult();
        _logger.LogInformation("开始物理清理任务，清理截止时间: {BeforeDate:o}", beforeDate);

        try
        {
            result.SharePostsCleanedCount = await CleanupSharePostsAsync(beforeDate, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理 SharePost 软删除记录时发生异常");
            result.HasErrors = true;
            result.Errors.Add($"清理 SharePost 异常: {ex.Message}");
        }

        try
        {
            result.ReservationsCleanedCount = await CleanupReservationsAsync(beforeDate, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理 Reservation 软删除记录时发生异常");
            result.HasErrors = true;
            result.Errors.Add($"清理 Reservation 异常: {ex.Message}");
        }

        try
        {
            result.PickupCodesCleanedCount = await CleanupPickupCodesAsync(beforeDate, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理 PickupCode 软删除记录时发生异常");
            result.HasErrors = true;
            result.Errors.Add($"清理 PickupCode 异常: {ex.Message}");
        }

        try
        {
            result.KarmaPointsCleanedCount = await CleanupKarmaPointsAsync(beforeDate, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理 KarmaPoint 软删除记录时发生异常");
            result.HasErrors = true;
            result.Errors.Add($"清理 KarmaPoint 异常: {ex.Message}");
        }

        try
        {
            result.SnapshotsCleanedCount = await CleanupSnapshotsAsync(beforeDate, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理 DeletedEntitySnapshot 记录时发生异常");
            result.HasErrors = true;
            result.Errors.Add($"清理 DeletedEntitySnapshot 异常: {ex.Message}");
        }

        result.Details["CleanupBeforeDate"] = beforeDate.ToString("o");
        result.Details["SharePostsCleaned"] = result.SharePostsCleanedCount;
        result.Details["ReservationsCleaned"] = result.ReservationsCleanedCount;
        result.Details["PickupCodesCleaned"] = result.PickupCodesCleanedCount;
        result.Details["KarmaPointsCleaned"] = result.KarmaPointsCleanedCount;
        result.Details["SnapshotsCleaned"] = result.SnapshotsCleanedCount;

        _logger.LogInformation(
            "物理清理任务执行完成: SharePost={Posts}, Reservation={Reservations}, PickupCode={Codes}, KarmaPoint={Points}, Snapshot={Snapshots}, Total={Total}, HasErrors={HasErrors}",
            result.SharePostsCleanedCount, result.ReservationsCleanedCount,
            result.PickupCodesCleanedCount, result.KarmaPointsCleanedCount,
            result.SnapshotsCleanedCount, result.TotalCleanedCount, result.HasErrors);

        return result;
    }

    /// <summary>
    /// 清理 SharePost 软删除记录
    /// </summary>
    private async Task<int> CleanupSharePostsAsync(DateTime beforeDate, HardCleanupResult result)
    {
        var expiredPosts = await _context.SharePosts
            .IgnoreQueryFilters()
            .Where(sp => sp.IsDeleted && sp.DeletedAt.HasValue && sp.DeletedAt.Value < beforeDate)
            .ToListAsync();

        if (expiredPosts.Count == 0)
        {
            _logger.LogDebug("没有需要清理的 SharePost 软删除记录");
            return 0;
        }

        _logger.LogInformation("找到 {Count} 条 SharePost 软删除记录需要物理清理", expiredPosts.Count);

        var postIds = expiredPosts.Select(sp => sp.Id).ToList();
        var relatedSnapshots = await _context.DeletedEntitySnapshots
            .Where(s => s.EntityType == nameof(SharePost) && postIds.Contains(s.EntityId))
            .ToListAsync();

        _unitOfWork.SharePosts.HardDeleteRange(expiredPosts);

        if (relatedSnapshots.Count > 0)
        {
            _context.DeletedEntitySnapshots.RemoveRange(relatedSnapshots);
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("已物理删除 {Count} 条 SharePost 记录，关联快照 {SnapshotCount} 条",
            expiredPosts.Count, relatedSnapshots.Count);

        return expiredPosts.Count;
    }

    /// <summary>
    /// 清理 Reservation 软删除记录
    /// </summary>
    private async Task<int> CleanupReservationsAsync(DateTime beforeDate, HardCleanupResult result)
    {
        var expiredReservations = await _context.Reservations
            .IgnoreQueryFilters()
            .Where(r => r.IsDeleted && r.DeletedAt.HasValue && r.DeletedAt.Value < beforeDate)
            .ToListAsync();

        if (expiredReservations.Count == 0)
        {
            _logger.LogDebug("没有需要清理的 Reservation 软删除记录");
            return 0;
        }

        _logger.LogInformation("找到 {Count} 条 Reservation 软删除记录需要物理清理", expiredReservations.Count);

        var reservationIds = expiredReservations.Select(r => r.Id).ToList();
        var relatedSnapshots = await _context.DeletedEntitySnapshots
            .Where(s => s.EntityType == nameof(Reservation) && reservationIds.Contains(s.EntityId))
            .ToListAsync();

        _unitOfWork.Reservations.HardDeleteRange(expiredReservations);

        if (relatedSnapshots.Count > 0)
        {
            _context.DeletedEntitySnapshots.RemoveRange(relatedSnapshots);
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("已物理删除 {Count} 条 Reservation 记录，关联快照 {SnapshotCount} 条",
            expiredReservations.Count, relatedSnapshots.Count);

        return expiredReservations.Count;
    }

    /// <summary>
    /// 清理 PickupCode 软删除记录
    /// </summary>
    private async Task<int> CleanupPickupCodesAsync(DateTime beforeDate, HardCleanupResult result)
    {
        var expiredCodes = await _context.PickupCodes
            .IgnoreQueryFilters()
            .Where(pc => pc.IsDeleted && pc.DeletedAt.HasValue && pc.DeletedAt.Value < beforeDate)
            .ToListAsync();

        if (expiredCodes.Count == 0)
        {
            _logger.LogDebug("没有需要清理的 PickupCode 软删除记录");
            return 0;
        }

        _logger.LogInformation("找到 {Count} 条 PickupCode 软删除记录需要物理清理", expiredCodes.Count);

        var codeIds = expiredCodes.Select(pc => pc.Id).ToList();
        var relatedSnapshots = await _context.DeletedEntitySnapshots
            .Where(s => s.EntityType == nameof(PickupCode) && codeIds.Contains(s.EntityId))
            .ToListAsync();

        _unitOfWork.PickupCodes.HardDeleteRange(expiredCodes);

        if (relatedSnapshots.Count > 0)
        {
            _context.DeletedEntitySnapshots.RemoveRange(relatedSnapshots);
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("已物理删除 {Count} 条 PickupCode 记录，关联快照 {SnapshotCount} 条",
            expiredCodes.Count, relatedSnapshots.Count);

        return expiredCodes.Count;
    }

    /// <summary>
    /// 清理 KarmaPoint 软删除记录
    /// </summary>
    private async Task<int> CleanupKarmaPointsAsync(DateTime beforeDate, HardCleanupResult result)
    {
        var expiredPoints = await _context.KarmaPoints
            .IgnoreQueryFilters()
            .Where(kp => kp.IsDeleted && kp.DeletedAt.HasValue && kp.DeletedAt.Value < beforeDate)
            .ToListAsync();

        if (expiredPoints.Count == 0)
        {
            _logger.LogDebug("没有需要清理的 KarmaPoint 软删除记录");
            return 0;
        }

        _logger.LogInformation("找到 {Count} 条 KarmaPoint 软删除记录需要物理清理", expiredPoints.Count);

        var pointIds = expiredPoints.Select(kp => kp.Id).ToList();
        var relatedSnapshots = await _context.DeletedEntitySnapshots
            .Where(s => s.EntityType == nameof(KarmaPoint) && pointIds.Contains(s.EntityId))
            .ToListAsync();

        _unitOfWork.KarmaPoints.HardDeleteRange(expiredPoints);

        if (relatedSnapshots.Count > 0)
        {
            _context.DeletedEntitySnapshots.RemoveRange(relatedSnapshots);
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("已物理删除 {Count} 条 KarmaPoint 记录，关联快照 {SnapshotCount} 条",
            expiredPoints.Count, relatedSnapshots.Count);

        return expiredPoints.Count;
    }

    /// <summary>
    /// 清理孤立的 DeletedEntitySnapshot 记录（没有对应实体或者已过期的）
    /// </summary>
    private async Task<int> CleanupSnapshotsAsync(DateTime beforeDate, HardCleanupResult result)
    {
        var orphanSnapshots = await _context.DeletedEntitySnapshots
            .Where(s => s.DeletedAt < beforeDate)
            .ToListAsync();

        if (orphanSnapshots.Count == 0)
        {
            _logger.LogDebug("没有需要清理的孤立快照记录");
            return 0;
        }

        _logger.LogInformation("找到 {Count} 条过期快照记录需要清理", orphanSnapshots.Count);

        _context.DeletedEntitySnapshots.RemoveRange(orphanSnapshots);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("已清理 {Count} 条过期快照记录", orphanSnapshots.Count);

        return orphanSnapshots.Count;
    }
}
