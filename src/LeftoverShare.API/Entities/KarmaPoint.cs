using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeftoverShare.API.Entities;

/// <summary>
/// 积分实体
/// </summary>
public class KarmaPoint : ISoftDeletable
{
    /// <summary>
    /// 积分ID
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    [Required]
    [ForeignKey(nameof(User))]
    public int UserId { get; set; }

    /// <summary>
    /// 用户
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// 积分数
    /// </summary>
    [Required]
    public int Points { get; set; }

    /// <summary>
    /// 原因
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// 关联ID
    /// </summary>
    public int? RelatedId { get; set; }

    /// <summary>
    /// 交易类型
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string TransactionType { get; set; } = "Earn";

    /// <summary>
    /// 创建时间
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

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
