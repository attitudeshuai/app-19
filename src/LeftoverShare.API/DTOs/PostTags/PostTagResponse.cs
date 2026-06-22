namespace LeftoverShare.API.DTOs.PostTags;

/// <summary>
/// 帖子标签响应DTO
/// </summary>
public class PostTagResponse
{
    /// <summary>
    /// 标签ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 标签名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 标签编码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 标签颜色
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// 标签图标URL
    /// </summary>
    public string? IconUrl { get; set; }

    /// <summary>
    /// 标签描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 使用次数
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// 是否为系统预设标签
    /// </summary>
    public bool IsSystemDefined { get; set; }

    /// <summary>
    /// 创建者用户ID（系统预设为null）
    /// </summary>
    public int? CreatedBy { get; set; }

    /// <summary>
    /// 创建者用户名
    /// </summary>
    public string? CreatedByUsername { get; set; }

    /// <summary>
    /// 排序权重
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
