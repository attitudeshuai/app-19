namespace LeftoverShare.API.DTOs.AllergenTags;

/// <summary>
/// 过敏原标签响应DTO
/// </summary>
public class AllergenTagResponse
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
    /// 过敏原图标URL
    /// </summary>
    public string? IconUrl { get; set; }

    /// <summary>
    /// 严重程度（1-低 2-中 3-高）
    /// </summary>
    public int SeverityLevel { get; set; }

    /// <summary>
    /// 严重程度描述
    /// </summary>
    public string SeverityLevelText => SeverityLevel switch
    {
        1 => "轻度",
        2 => "中度",
        3 => "重度",
        _ => "未知"
    };

    /// <summary>
    /// 详细说明
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 排序权重
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// 关联分享帖数量
    /// </summary>
    public int PostCount { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
