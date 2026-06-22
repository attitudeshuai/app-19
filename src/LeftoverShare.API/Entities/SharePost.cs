using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LeftoverShare.API.Entities.Enums;

namespace LeftoverShare.API.Entities;

/// <summary>
/// 分享帖子实体
/// </summary>
public class SharePost : ISoftDeletable
{
    /// <summary>
    /// 帖子ID
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// 发布者ID
    /// </summary>
    [Required]
    [ForeignKey(nameof(Poster))]
    public int PosterId { get; set; }

    /// <summary>
    /// 发布者
    /// </summary>
    public virtual User Poster { get; set; } = null!;

    /// <summary>
    /// 标题
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 描述
    /// </summary>
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 食物类型
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string FoodType { get; set; } = string.Empty;

    /// <summary>
    /// 数量
    /// </summary>
    [Required]
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// 取餐地址
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string PickupAddress { get; set; } = string.Empty;

    /// <summary>
    /// 纬度
    /// </summary>
    [Column(TypeName = "decimal(9, 6)")]
    public decimal? Latitude { get; set; }

    /// <summary>
    /// 经度
    /// </summary>
    [Column(TypeName = "decimal(9, 6)")]
    public decimal? Longitude { get; set; }

    /// <summary>
    /// 可领取截止时间
    /// </summary>
    [Required]
    public DateTime AvailableUntil { get; set; }

    /// <summary>
    /// 照片（JSON序列化列表）
    /// </summary>
    [MaxLength(2000)]
    public string? Photos { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [Required]
    public SharePostStatus Status { get; set; } = SharePostStatus.Available;

    /// <summary>
    /// 创建时间
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 失效原因
    /// </summary>
    public ExpirationReason? ExpirationReason { get; set; }

    /// <summary>
    /// 失效操作时间
    /// </summary>
    public DateTime? ExpiredAt { get; set; }

    /// <summary>
    /// 预订记录
    /// </summary>
    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

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
