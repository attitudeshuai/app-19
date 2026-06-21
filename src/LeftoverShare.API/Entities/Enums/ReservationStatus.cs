namespace LeftoverShare.API.Entities.Enums;

/// <summary>
/// 预订状态枚举
/// </summary>
public enum ReservationStatus
{
    /// <summary>
    /// 待确认
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 已确认
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// 已完成
    /// </summary>
    Completed = 2,

    /// <summary>
    /// 已取消
    /// </summary>
    Cancelled = 3
}
