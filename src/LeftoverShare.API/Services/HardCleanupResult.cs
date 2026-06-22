namespace LeftoverShare.API.Services;

/// <summary>
/// 物理清理任务执行结果
/// </summary>
public class HardCleanupResult
{
    /// <summary>
    /// 物理删除的分享帖数量
    /// </summary>
    public int SharePostsCleanedCount { get; set; }

    /// <summary>
    /// 物理删除的预约数量
    /// </summary>
    public int ReservationsCleanedCount { get; set; }

    /// <summary>
    /// 物理删除的取餐码数量
    /// </summary>
    public int PickupCodesCleanedCount { get; set; }

    /// <summary>
    /// 物理删除的积分流水数量
    /// </summary>
    public int KarmaPointsCleanedCount { get; set; }

    /// <summary>
    /// 清理的快照记录数量
    /// </summary>
    public int SnapshotsCleanedCount { get; set; }

    /// <summary>
    /// 物理删除的实体总数
    /// </summary>
    public int TotalCleanedCount => SharePostsCleanedCount + ReservationsCleanedCount
                                  + PickupCodesCleanedCount + KarmaPointsCleanedCount;

    /// <summary>
    /// 处理过程中是否有错误
    /// </summary>
    public bool HasErrors { get; set; }

    /// <summary>
    /// 错误信息集合
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// 处理详情（用于日志审计）
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();
}
