using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeftoverShare.API.Entities;

/// <summary>
/// 分享帖-帖子标签关联实体（多对多中间表）
/// 业务意图：建立分享帖和帖子标签的多对多关系
/// </summary>
public class SharePostPostTag
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
    /// 帖子标签ID
    /// </summary>
    [Required]
    [ForeignKey(nameof(PostTag))]
    public int PostTagId { get; set; }

    /// <summary>
    /// 帖子标签
    /// </summary>
    public virtual PostTag PostTag { get; set; } = null!;

    /// <summary>
    /// 创建时间
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
