namespace LeftoverShare.API.Entities.Enums;

/// <summary>
/// 通知类型枚举
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// 分享帖过期通知
    /// </summary>
    SharePostExpired = 0,

    /// <summary>
    /// 取餐码超时未使用通知
    /// </summary>
    PickupCodeExpired = 1,

    /// <summary>
    /// 系统通知
    /// </summary>
    System = 2
}
