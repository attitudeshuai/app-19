namespace LeftoverShare.API.Services;

/// <summary>
/// 过期清理处理服务接口
/// </summary>
public interface IExpirationProcessingService
{
    /// <summary>
    /// 执行过期清理任务：标记过期分享帖和超时取餐码为失效，并发送通知
    /// </summary>
    /// <returns>处理结果</returns>
    Task<ExpirationProcessingResult> ProcessExpiredItemsAsync();
}
