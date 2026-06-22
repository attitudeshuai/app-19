using LeftoverShare.API.Entities;

namespace LeftoverShare.API.Repositories;

/// <summary>
/// 食物分类仓储接口
/// </summary>
public interface IFoodCategoryRepository : IRepository<FoodCategory>
{
    /// <summary>
    /// 根据编码获取分类
    /// </summary>
    Task<FoodCategory?> GetByCodeAsync(string code);

    /// <summary>
    /// 获取所有顶级分类（ParentId为null）
    /// </summary>
    Task<List<FoodCategory>> GetRootCategoriesAsync(bool includeInactive = false);

    /// <summary>
    /// 根据父级ID获取子分类
    /// </summary>
    Task<List<FoodCategory>> GetChildrenAsync(int parentId, bool includeInactive = false);

    /// <summary>
    /// 获取所有启用的分类（带层级）
    /// </summary>
    Task<List<FoodCategory>> GetAllActiveWithHierarchyAsync();

    /// <summary>
    /// 检查分类是否有子分类
    /// </summary>
    Task<bool> HasChildrenAsync(int categoryId);

    /// <summary>
    /// 检查分类是否被帖子使用
    /// </summary>
    Task<bool> IsUsedByPostsAsync(int categoryId);

    /// <summary>
    /// 获取指定分类的所有后代分类ID（包括子、孙、曾孙...）
    /// 用于循环引用检测，防止将父级设为自身后代
    /// </summary>
    Task<List<int>> GetAllDescendantIdsAsync(int categoryId);

    /// <summary>
    /// 获取指定分类的所有祖先分类ID（包括父、祖父、曾祖父...）
    /// 用于循环引用检测，确保父级所在树结构健康
    /// </summary>
    Task<List<int>> GetAllAncestorIdsAsync(int categoryId);
}
