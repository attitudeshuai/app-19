namespace LeftoverShare.API.DTOs.AllergenTags;

/// <summary>
/// 更新过敏原标签请求DTO
/// </summary>
public class UpdateAllergenTagRequest
{
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
}
