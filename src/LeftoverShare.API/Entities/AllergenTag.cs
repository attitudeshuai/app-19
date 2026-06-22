using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeftoverShare.API.Entities;

/// <summary>
/// 过敏原标签实体
/// 业务意图：管理食物过敏原标签（如花生、海鲜、乳制品等），用于食品安全标识和过敏原过滤
/// </summary>
public class AllergenTag : ISoftDeletable
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
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 标签编码（英文唯一标识）
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 过敏原图标URL（警示图标）
    /// </summary>
    [MaxLength(500)]
    public string? IconUrl { get; set; }

    /// <summary>
    /// 严重程度（1-低 2-中 3-高）
    /// </summary>
    [Required]
    public int SeverityLevel { get; set; } = 2;

    /// <summary>
    /// 过敏原详细说明
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

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
    /// 关联的分享帖（多对多通过中间表
    /// </summary>
    public virtual ICollection<SharePostAllergenTag> SharePostAllergenTags { get; set; } = new List<SharePostAllergenTag>();

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
