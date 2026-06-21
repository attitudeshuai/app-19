using LeftoverShare.API.Entities.Enums;

namespace LeftoverShare.API.Services;

/// <summary>
/// 站内通知服务接口
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// 发送通知给指定用户
    /// </summary>
    /// <param name="userId">接收用户ID</param>
    /// <param name="type">通知类型</param>
    /// <param name="title">通知标题</param>
    /// <param name="content">通知内容</param>
    /// <param name="sharePostId">关联的分享帖ID（可选）</param>
    /// <param name="reservationId">关联的预订ID（可选）</param>
    /// <returns>是否发送成功</returns>
    Task<bool> SendNotificationAsync(
        int userId,
        NotificationType type,
        string title,
        string content,
        int? sharePostId = null,
        int? reservationId = null);

    /// <summary>
    /// 批量发送通知
    /// </summary>
    /// <param name="notifications">通知数据集合</param>
    /// <returns>成功发送的数量</returns>
    Task<int> SendBulkNotificationsAsync(IEnumerable<(int UserId, NotificationType Type, string Title, string Content, int? SharePostId, int? ReservationId)> notifications);
}
