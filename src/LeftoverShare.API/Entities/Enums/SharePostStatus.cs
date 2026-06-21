namespace LeftoverShare.API.Entities.Enums;

/// <summary>
/// 分享帖子状态枚举
/// </summary>
public enum SharePostStatus
{
    /// <summary>
    /// 可领取
    /// </summary>
    Available = 0,

    /// <summary>
    /// 已预订
    /// </summary>
    Reserved = 1,

    /// <summary>
    /// 已取餐
    /// </summary>
    PickedUp = 2,

    /// <summary>
    /// 已过期
    /// </summary>
    Expired = 3,

    /// <summary>
    /// 已完成
    /// </summary>
    Completed = 4
}
