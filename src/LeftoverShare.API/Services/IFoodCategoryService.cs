using LeftoverShare.API.DTOs.FoodCategories;
using LeftoverShare.API.Helpers;

namespace LeftoverShare.API.Services;

/// <summary>
/// 食物分类服务接口
/// </summary>
public interface IFoodCategoryService
{
    /// <summary>
    /// 获取所有分类（树形结构）
    /// </summary>
    Task<ApiResponse> GetTreeAsync(bool includeInactive = false);

    /// <summary>
    /// 获取所有顶级分类
    /// </summary>
    Task<ApiResponse> GetRootCategoriesAsync(bool includeInactive = false);

    /// <summary>
    /// 获取分类的子分类
    /// </summary>
    Task<ApiResponse> GetChildrenAsync(int parentId, bool includeInactive = false);

    /// <summary>
    /// 根据ID获取分类详情
    /// </summary>
    Task<ApiResponse> GetByIdAsync(int id);

    /// <summary>
    /// 创建分类（管理员）
    /// </summary>
    Task<ApiResponse> CreateAsync(int currentUserId, CreateFoodCategoryRequest request);

    /// <summary>
    /// 更新分类（管理员）
    /// </summary>
    Task<ApiResponse> UpdateAsync(int id, int currentUserId, UpdateFoodCategoryRequest request);

    /// <summary>
    /// 删除分类（管理员，软删除）
    /// </summary>
    Task<ApiResponse> DeleteAsync(int id, int currentUserId);

    /// <summary>
    /// 启用/禁用分类
    /// </summary>
    Task<ApiResponse> ToggleActiveAsync(int id, int currentUserId, bool isActive);

    /// <summary>
    /// 批量调整排序
    /// </summary>
    Task<ApiResponse> UpdateSortOrderAsync(int currentUserId, Dictionary<int, int> sortOrders);
}
