using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeftoverShare.API.Entities;

/// <summary>
/// 帖子标签实体
/// 业务意图：自由标签，可灵活打在分享帖上（如"急出"、"限自提"、"免费"等）
/// </summary>
public class PostTag : ISoftDeletable
{
    /// <summary>
    /// 标签ID
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// 标签名称
    /// </summary>
    [Required]
    [MaxLength(30)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 标签编码（英文唯一标识）
    /// </summary>
    [Required]
    [MaxLength(30)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 标签颜色（前端显示）
    /// </summary>
    [MaxLength(20)]
    public string? Color { get; set; } = "#3B82F6";

    /// <summary>
    /// 标签图标URL
    /// </summary>
    [MaxLength(500)]
    public string? IconUrl { get; set; }

    /// <summary>
    /// 标签描述
    /// </summary>
    [MaxLength(200)]
    public string? Description { get; set; }

    /// <summary>
    /// 使用次数统计（用于热门标签排序）
    /// </summary>
    public int UsageCount { get; set; } = 0;

    /// <summary>
    /// 是否为系统预设标签（true-管理员创建，false-用户创建）
    /// </summary>
    public bool IsSystemDefined { get; set; } = false;

    /// <summary>
    /// 创建者用户ID（系统预设为null）
    /// </summary>
    [ForeignKey(nameof(CreatedByUser))]
    public int? CreatedBy { get; set; }

    /// <summary>
    /// 创建者用户
    /// </summary>
    public virtual User? CreatedByUser { get; set; }

    /// <summary>
    /// 排序权重
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 关联的分享帖（多对多中间表）
    /// </summary>
    public virtual ICollection<SharePostPostTag> SharePostPostTags { get; set; } = new List<SharePostPostTag>();

    /// <summary>
    /// 是否已软删除
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// 软删除时间
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// 软删除操作者ID
    /// </summary>
    public int? DeletedBy { get; set; }

    /// <summary>
    /// 删除原因
    /// </summary>
    [MaxLength(500)]
    public string? DeletionReason { get; set; }
}
