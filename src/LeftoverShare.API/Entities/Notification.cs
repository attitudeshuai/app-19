using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LeftoverShare.API.Entities.Enums;

namespace LeftoverShare.API.Entities;

/// <summary>
/// 站内通知实体
/// </summary>
public class Notification
{
    /// <summary>
    /// 通知ID
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// 接收用户ID
    /// </summary>
    [Required]
    [ForeignKey(nameof(User))]
    public int UserId { get; set; }

    /// <summary>
    /// 接收用户
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// 通知类型
    /// </summary>
    [Required]
    public NotificationType Type { get; set; }

    /// <summary>
    /// 通知标题
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 通知内容
    /// </summary>
    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 关联的分享帖ID（可为空）
    /// </summary>
    [ForeignKey(nameof(SharePost))]
    public int? SharePostId { get; set; }

    /// <summary>
    /// 关联的分享帖
    /// </summary>
    public virtual SharePost? SharePost { get; set; }

    /// <summary>
    /// 关联的预订ID（可为空）
    /// </summary>
    [ForeignKey(nameof(Reservation))]
    public int? ReservationId { get; set; }

    /// <summary>
    /// 关联的预订
    /// </summary>
    public virtual Reservation? Reservation { get; set; }

    /// <summary>
    /// 是否已读
    /// </summary>
    public bool IsRead { get; set; } = false;

    /// <summary>
    /// 阅读时间
    /// </summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
