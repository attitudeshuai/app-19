using LeftoverShare.API.DTOs.AllergenTags;
using LeftoverShare.API.Helpers;

namespace LeftoverShare.API.Services;

/// <summary>
/// 过敏原标签服务接口
/// </summary>
public interface IAllergenTagService
{
    /// <summary>
    /// 获取所有启用的标签
    /// </summary>
    Task<ApiResponse> GetAllActiveAsync();

    /// <summary>
    /// 分页获取标签列表（管理员）
    /// </summary>
    Task<ApiResponse> GetPagedAsync(int pageNumber, int pageSize, bool includeInactive = false, int? severityLevel = null, string? searchTerm = null);

    /// <summary>
    /// 根据ID获取标签详情
    /// </summary>
    Task<ApiResponse> GetByIdAsync(int id);

    /// <summary>
    /// 创建标签（管理员）
    /// </summary>
    Task<ApiResponse> CreateAsync(int currentUserId, CreateAllergenTagRequest request);

    /// <summary>
    /// 更新标签（管理员）
    /// </summary>
    Task<ApiResponse> UpdateAsync(int id, int currentUserId, UpdateAllergenTagRequest request);

    /// <summary>
    /// 删除标签（管理员，软删除）
    /// </summary>
    Task<ApiResponse> DeleteAsync(int id, int currentUserId);

    /// <summary>
    /// 启用/禁用标签
    /// </summary>
    Task<ApiResponse> ToggleActiveAsync(int id, int currentUserId, bool isActive);

    /// <summary>
    /// 批量调整排序
    /// </summary>
    Task<ApiResponse> UpdateSortOrderAsync(int currentUserId, Dictionary<int, int> sortOrders);
}
