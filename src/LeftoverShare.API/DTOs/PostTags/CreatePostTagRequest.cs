namespace LeftoverShare.API.DTOs.PostTags;

/// <summary>
/// 创建帖子标签请求DTO
/// </summary>
public class CreatePostTagRequest
{
    /// <summary>
    /// 标签名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 标签编码（英文唯一标识）
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 标签颜色（十六进制，如#3B82F6）
    /// </summary>
    public string? Color { get; set; } = "#3B82F6";

    /// <summary>
    /// 标签图标URL
    /// </summary>
    public string? IconUrl { get; set; }

    /// <summary>
    /// 标签描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 排序权重
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsActive { get; set; } = true;
}
