using LeftoverShare.API.DTOs.PostTags;
using LeftoverShare.API.Helpers;

namespace LeftoverShare.API.Services;

/// <summary>
/// 帖子标签服务接口
/// </summary>
public interface IPostTagService
{
    /// <summary>
    /// 获取所有启用的标签（含用户自定义）
    /// </summary>
    Task<ApiResponse> GetAllActiveAsync(bool includeUserDefined = true);

    /// <summary>
    /// 获取热门标签
    /// </summary>
    Task<ApiResponse> GetPopularAsync(int topN = 20);

    /// <summary>
    /// 分页获取标签列表（管理员）
    /// </summary>
    Task<ApiResponse> GetPagedAsync(int pageNumber, int pageSize, bool includeInactive = false, bool? isSystemDefined = null, int? createdBy = null, string? searchTerm = null);

    /// <summary>
    /// 根据ID获取标签详情
    /// </summary>
    Task<ApiResponse> GetByIdAsync(int id);

    /// <summary>
    /// 搜索标签（用于自动补全）
    /// </summary>
    Task<ApiResponse> SearchAsync(string keyword, int limit = 20);

    /// <summary>
    /// 获取当前用户创建的标签
    /// </summary>
    Task<ApiResponse> GetMyTagsAsync(int userId);

    /// <summary>
    /// 创建系统预设标签（管理员）
    /// </summary>
    Task<ApiResponse> CreateSystemTagAsync(int currentUserId, CreatePostTagRequest request);

    /// <summary>
    /// 用户创建自定义标签
    /// </summary>
    Task<ApiResponse> CreateUserTagAsync(int currentUserId, CreatePostTagRequest request);

    /// <summary>
    /// 更新标签（管理员或创建者）
    /// </summary>
    Task<ApiResponse> UpdateAsync(int id, int currentUserId, UpdatePostTagRequest request);

    /// <summary>
    /// 删除标签（管理员或创建者）
    /// </summary>
    Task<ApiResponse> DeleteAsync(int id, int currentUserId);

    /// <summary>
    /// 启用/禁用标签（管理员）
    /// </summary>
    Task<ApiResponse> ToggleActiveAsync(int id, int currentUserId, bool isActive);

    /// <summary>
    /// 批量调整排序（管理员）
    /// </summary>
    Task<ApiResponse> UpdateSortOrderAsync(int currentUserId, Dictionary<int, int> sortOrders);
}
