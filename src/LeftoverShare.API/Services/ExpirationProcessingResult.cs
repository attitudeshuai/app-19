namespace LeftoverShare.API.Services;

/// <summary>
/// 过期清理任务执行结果
/// </summary>
public class ExpirationProcessingResult
{
    /// <summary>
    /// 处理的过期分享帖数量
    /// </summary>
    public int ExpiredSharePostsCount { get; set; }

    /// <summary>
    /// 处理的超时取餐码数量
    /// </summary>
    public int ExpiredPickupCodesCount { get; set; }

    /// <summary>
    /// 发送的通知总数
    /// </summary>
    public int NotificationsSentCount { get; set; }

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
