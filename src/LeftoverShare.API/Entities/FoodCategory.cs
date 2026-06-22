using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeftoverShare.API.Entities;

/// <summary>
/// 食物分类实体
/// 业务意图：管理食物的分类体系，支持层级结构（如中餐→川菜→鱼香肉丝），用于搜索过滤和帖子分类
/// </summary>
public class FoodCategory : ISoftDeletable
{
    /// <summary>
    /// 分类ID
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// 分类名称
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 分类编码（英文唯一标识，用于前端快速索引）
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 父级分类ID（顶级分类为null）
    /// </summary>
    [ForeignKey(nameof(Parent))]
    public int? ParentId { get; set; }

    /// <summary>
    /// 父级分类
    /// </summary>
    public virtual FoodCategory? Parent { get; set; }

    /// <summary>
    /// 子分类集合
    /// </summary>
    public virtual ICollection<FoodCategory> Children { get; set; } = new List<FoodCategory>();

    /// <summary>
    /// 分类图标URL
    /// </summary>
    [MaxLength(500)]
    public string? IconUrl { get; set; }

    /// <summary>
    /// 排序权重（数值越小越靠前）
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// 分类描述
    /// </summary>
    [MaxLength(200)]
    public string? Description { get; set; }

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
    /// 使用该分类的分享帖集合
    /// </summary>
    public virtual ICollection<SharePost> SharePosts { get; set; } = new List<SharePost>();

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
