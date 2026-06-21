using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LeftoverShare.API.Entities.Enums;

namespace LeftoverShare.API.Entities;

/// <summary>
/// 定时任务执行日志实体
/// </summary>
public class ScheduledTaskLog
{
    /// <summary>
    /// 日志ID
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// 任务名称
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string TaskName { get; set; } = string.Empty;

    /// <summary>
    /// 任务执行状态
    /// </summary>
    [Required]
    public ScheduledTaskStatus Status { get; set; }

    /// <summary>
    /// 处理的过期分享帖数量
    /// </summary>
    public int ExpiredSharePostsCount { get; set; }

    /// <summary>
    /// 处理的超时取餐码数量
    /// </summary>
    public int ExpiredPickupCodesCount { get; set; }

    /// <summary>
    /// 发送的通知数量
    /// </summary>
    public int NotificationsSentCount { get; set; }

    /// <summary>
    /// 开始执行时间
    /// </summary>
    [Required]
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// 执行耗时（毫秒）
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// 错误信息（如果有）
    /// </summary>
    [MaxLength(4000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 详细日志（JSON格式）
    /// </summary>
    [MaxLength(8000)]
    public string? Details { get; set; }
}
