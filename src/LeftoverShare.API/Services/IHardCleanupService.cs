namespace LeftoverShare.API.Services;

/// <summary>
/// 物理清理服务接口
/// 负责清理过期的软删除记录（物理删除）
/// </summary>
public interface IHardCleanupService
{
    /// <summary>
    /// 清理指定日期之前的软删除记录（物理删除）
    /// 按 SharePost > Reservation > PickupCode > KarmaPoint 顺序处理，避免外键约束
    /// </summary>
    /// <param name="beforeDate">删除此日期之前的软删除记录</param>
    /// <returns>物理清理结果</returns>
    Task<HardCleanupResult> CleanupExpiredSoftDeletesAsync(DateTime beforeDate);
}
