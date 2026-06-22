using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeftoverShare.API.Entities;

/// <summary>
/// 分享帖-过敏原标签关联实体（多对多中间表）
/// 业务意图：建立分享帖和过敏原标签的多对多关系
/// </summary>
public class SharePostAllergenTag
{
    /// <summary>
    /// 分享帖ID
    /// </summary>
    [Required]
    [ForeignKey(nameof(SharePost))]
    public int SharePostId { get; set; }

    /// <summary>
    /// 分享帖
    /// </summary>
    public virtual SharePost SharePost { get; set; } = null!;

    /// <summary>
    /// 过敏原标签ID
    /// </summary>
    [Required]
    [ForeignKey(nameof(AllergenTag))]
    public int AllergenTagId { get; set; }

    /// <summary>
    /// 过敏原标签
    /// </summary>
    public virtual AllergenTag AllergenTag { get; set; } = null!;

    /// <summary>
    /// 创建时间
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
