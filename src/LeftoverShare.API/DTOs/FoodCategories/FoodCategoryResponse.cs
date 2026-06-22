namespace LeftoverShare.API.DTOs.FoodCategories;

/// <summary>
/// 食物分类响应DTO
/// </summary>
public class FoodCategoryResponse
{
    /// <summary>
    /// 分类ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 分类名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 分类编码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 父级分类ID
    /// </summary>
    public int? ParentId { get; set; }

    /// <summary>
    /// 分类图标URL
    /// </summary>
    public string? IconUrl { get; set; }

    /// <summary>
    /// 排序权重
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 分类描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// 子分类列表
    /// </summary>
    public List<FoodCategoryResponse>? Children { get; set; }

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
