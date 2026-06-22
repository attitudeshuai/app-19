using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LeftoverShare.API.Entities.Enums;

namespace LeftoverShare.API.Entities;

/// <summary>
/// 预订实体
/// </summary>
public class Reservation : ISoftDeletable
{
    /// <summary>
    /// 预订ID
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// 帖子ID
    /// </summary>
    [Required]
    [ForeignKey(nameof(Post))]
    public int PostId { get; set; }

    /// <summary>
    /// 帖子
    /// </summary>
    public virtual SharePost Post { get; set; } = null!;

    /// <summary>
    /// 领取者ID
    /// </summary>
    [Required]
    [ForeignKey(nameof(Claimer))]
    public int ClaimerId { get; set; }

    /// <summary>
    /// 领取者
    /// </summary>
    public virtual User Claimer { get; set; } = null!;

    /// <summary>
    /// 数量
    /// </summary>
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// 预订时间
    /// </summary>
    [Required]
    public DateTime ReservedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 确认时间
    /// </summary>
    public DateTime? ConfirmedAt { get; set; }

    /// <summary>
    /// 取餐时间
    /// </summary>
    public DateTime? PickedUpAt { get; set; }

    /// <summary>
    /// 取餐码
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string PickupCode { get; set; } = string.Empty;

    /// <summary>
    /// 状态
    /// </summary>
    [Required]
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

    /// <summary>
    /// 备注
    /// </summary>
    [MaxLength(500)]
    public string? Note { get; set; }

    /// <summary>
    /// 取餐码导航属性
    /// </summary>
    public virtual PickupCode? PickupCodeNavigation { get; set; }

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
