using LeftoverShare.API.Entities.Enums;

namespace LeftoverShare.API.Helpers.Exceptions;

/// <summary>
/// 预约业务异常基类
/// 业务意图：用于封装预约相关的业务异常，
/// 包含错误码、错误消息和可选的详细数据
/// </summary>
public class ReservationException : ApplicationException
{
    /// <summary>
    /// 错误码
    /// </summary>
    public ReservationErrorCode ErrorCode { get; }

    /// <summary>
    /// 错误详情，用于返回给调用方的额外信息
    /// </summary>
    public IDictionary<string, object>? Details { get; }

    /// <summary>
    /// 创建预约异常
    /// </summary>
    /// <param name="errorCode">错误码</param>
    /// <param name="message">错误消息</param>
    /// <param name="details">错误详情</param>
    public ReservationException(
        ReservationErrorCode errorCode,
        string message,
        IDictionary<string, object>? details = null)
        : base(message)
    {
        ErrorCode = errorCode;
        Details = details;
    }

    /// <summary>
    /// 创建预约异常（包含内部异常）
    /// </summary>
    /// <param name="errorCode">错误码</param>
    /// <param name="message">错误消息</param>
    /// <param name="innerException">内部异常</param>
    /// <param name="details">错误详情</param>
    public ReservationException(
        ReservationErrorCode errorCode,
        string message,
        Exception innerException,
        IDictionary<string, object>? details = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Details = details;
    }

    /// <summary>
    /// 创建库存不足异常
    /// </summary>
    public static ReservationException InsufficientStock(int postId, int available, int requested)
    {
        var details = new Dictionary<string, object>
        {
            ["postId"] = postId,
            ["availableQuantity"] = available,
            ["requestedQuantity"] = requested
        };

        return new ReservationException(
            ReservationErrorCode.InsufficientStock,
            $"库存不足，剩余可用数量为 {available}，请求数量为 {requested}",
            details);
    }

    /// <summary>
    /// 创建重复预约异常
    /// </summary>
    public static ReservationException DuplicateReservation(int postId, int userId)
    {
        var details = new Dictionary<string, object>
        {
            ["postId"] = postId,
            ["userId"] = userId,
            ["existingReservation"] = true
        };

        return new ReservationException(
            ReservationErrorCode.DuplicateReservation,
            "您已预约过此分享帖，请勿重复预约",
            details);
    }

    /// <summary>
    /// 创建并发冲突异常
    /// </summary>
    public static ReservationException ConcurrencyConflict(int postId, int retryCount = 0)
    {
        var details = new Dictionary<string, object>
        {
            ["postId"] = postId,
            ["retryCount"] = retryCount,
            ["canRetry"] = retryCount < 3
        };

        return new ReservationException(
            ReservationErrorCode.ConcurrencyConflict,
            "操作过于频繁，数据已被其他用户修改，请稍后重试",
            details);
    }

    /// <summary>
    /// 创建预约超时异常
    /// </summary>
    public static ReservationException ReservationTimeout(int postId, int maxRetries)
    {
        var details = new Dictionary<string, object>
        {
            ["postId"] = postId,
            ["maxRetries"] = maxRetries,
            ["suggestion"] = "请稍后再试，或选择其他分享帖"
        };

        return new ReservationException(
            ReservationErrorCode.ReservationTimeout,
            $"系统繁忙，{maxRetries} 次重试后仍未完成预约，请稍后再试",
            details);
    }

    /// <summary>
    /// 创建不能预约自己帖子的异常
    /// </summary>
    public static ReservationException CannotReserveOwnPost(int postId, int userId)
    {
        var details = new Dictionary<string, object>
        {
            ["postId"] = postId,
            ["userId"] = userId
        };

        return new ReservationException(
            ReservationErrorCode.CannotReserveOwnPost,
            "不能预约自己发布的分享帖",
            details);
    }

    /// <summary>
    /// 创建分享帖不可预约异常
    /// </summary>
    public static ReservationException PostNotAvailable(int postId, string currentStatus)
    {
        var details = new Dictionary<string, object>
        {
            ["postId"] = postId,
            ["currentStatus"] = currentStatus
        };

        return new ReservationException(
            ReservationErrorCode.PostNotAvailable,
            $"该分享帖当前状态为 {currentStatus}，不可预约",
            details);
    }

    /// <summary>
    /// 创建分享帖不存在异常
    /// </summary>
    public static ReservationException PostNotFound(int postId)
    {
        var details = new Dictionary<string, object>
        {
            ["postId"] = postId
        };

        return new ReservationException(
            ReservationErrorCode.PostNotFound,
            $"分享帖 {postId} 不存在",
            details);
    }
}
