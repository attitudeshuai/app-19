namespace LeftoverShare.API.Entities.Enums;

/// <summary>
/// 失效原因枚举
/// </summary>
public enum ExpirationReason
{
    /// <summary>
    /// 超过有效期
    /// </summary>
    PastExpiryTime = 0,

    /// <summary>
    /// 取餐码超过24小时未使用
    /// </summary>
    PickupCodeUnusedOver24h = 1
}
