using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LeftoverShare.API.Entities.Enums;

namespace LeftoverShare.API.Entities;

/// <summary>
/// 取餐码实体
/// </summary>
public class PickupCode : ISoftDeletable
{
    /// <summary>
    /// 取餐码ID
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// 预订ID
    /// </summary>
    [Required]
    [ForeignKey(nameof(Reservation))]
    public int ReservationId { get; set; }

    /// <summary>
    /// 预订记录
    /// </summary>
    public virtual Reservation Reservation { get; set; } = null!;

    /// <summary>
    /// 取餐码
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 过期时间
    /// </summary>
    [Required]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// 是否已使用
    /// </summary>
    public bool IsUsed { get; set; } = false;

    /// <summary>
    /// 使用时间
    /// </summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>
    /// 是否已失效
    /// </summary>
    public bool IsExpired { get; set; } = false;

    /// <summary>
    /// 失效原因
    /// </summary>
    public ExpirationReason? ExpirationReason { get; set; }

    /// <summary>
    /// 失效操作时间
    /// </summary>
    public DateTime? ExpiredAt { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
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
