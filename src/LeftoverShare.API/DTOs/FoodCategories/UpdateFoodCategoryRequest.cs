namespace LeftoverShare.API.DTOs.FoodCategories;

/// <summary>
/// 更新食物分类请求DTO
/// </summary>
public class UpdateFoodCategoryRequest
{
    /// <summary>
    /// 分类名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 分类编码（英文唯一标识）
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 父级分类ID（顶级分类为null）
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
}
