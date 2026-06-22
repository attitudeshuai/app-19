namespace LeftoverShare.API.Entities.Enums;

/// <summary>
/// 预约业务错误码枚举
/// 业务意图：定义高并发预约场景下的所有可能错误码，
/// 便于调用方识别和处理不同类型的预约失败场景
/// </summary>
public enum ReservationErrorCode
{
    /// <summary>
    /// 未知错误
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// 分享帖不存在
    /// </summary>
    PostNotFound = 1001,

    /// <summary>
    /// 分享帖当前不可预约（状态非Available）
    /// </summary>
    PostNotAvailable = 1002,

    /// <summary>
    /// 不能预约自己发布的分享帖
    /// </summary>
    CannotReserveOwnPost = 1003,

    /// <summary>
    /// 库存不足，剩余可用数量为0
    /// </summary>
    InsufficientStock = 2001,

    /// <summary>
    /// 请求数量超过剩余可用数量
    /// </summary>
    QuantityExceedsAvailable = 2002,

    /// <summary>
    /// 该用户已预约过此分享帖
    /// </summary>
    DuplicateReservation = 3001,

    /// <summary>
    /// 并发冲突，数据已被其他请求修改
    /// </summary>
    ConcurrencyConflict = 4001,

    /// <summary>
    /// 预约操作超时（多次重试后仍失败）
    /// </summary>
    ReservationTimeout = 4002,

    /// <summary>
    /// 系统繁忙，请稍后重试
    /// </summary>
    SystemBusy = 4003,

    /// <summary>
    /// 预约不存在
    /// </summary>
    ReservationNotFound = 5001,

    /// <summary>
    /// 无权限操作此预约
    /// </summary>
    PermissionDenied = 5002,

    /// <summary>
    /// 预约状态不允许此操作
    /// </summary>
    InvalidStatusTransition = 5003
}
