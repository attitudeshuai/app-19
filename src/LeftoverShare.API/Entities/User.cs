using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LeftoverShare.API.Entities.Enums;

namespace LeftoverShare.API.Entities;

/// <summary>
/// 用户实体
/// </summary>
public class User
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 邮箱
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 密码哈希
    /// </summary>
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// 头像
    /// </summary>
    [MaxLength(200)]
    public string? Avatar { get; set; }

    /// <summary>
    /// 电话
    /// </summary>
    [MaxLength(20)]
    public string? Phone { get; set; }

    /// <summary>
    /// 地址
    /// </summary>
    [MaxLength(200)]
    public string? Address { get; set; }

    /// <summary>
    /// 用户角色
    /// </summary>
    [Required]
    public UserRole Role { get; set; } = UserRole.User;

    /// <summary>
    /// 总积分
    /// </summary>
    public int TotalKarmaPoints { get; set; } = 0;

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 发布的分享帖子
    /// </summary>
    public virtual ICollection<SharePost> SharePosts { get; set; } = new List<SharePost>();

    /// <summary>
    /// 预订记录
    /// </summary>
    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    /// <summary>
    /// 积分记录
    /// </summary>
    public virtual ICollection<KarmaPoint> KarmaPoints { get; set; } = new List<KarmaPoint>();

    /// <summary>
    /// 创建的帖子标签（仅用户自定义标签）
    /// </summary>
    public virtual ICollection<PostTag> CreatedPostTags { get; set; } = new List<PostTag>();
}
