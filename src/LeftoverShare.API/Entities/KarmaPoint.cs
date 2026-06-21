using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeftoverShare.API.Entities;

/// <summary>
/// 积分实体
/// </summary>
public class KarmaPoint
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
}
