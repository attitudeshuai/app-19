using LeftoverShare.API.DTOs.Common;
using LeftoverShare.API.DTOs.SharePosts;
using LeftoverShare.API.Helpers;

namespace LeftoverShare.API.Services;

/// <summary>
/// 分享帖服务接口
/// 业务意图：定义分享帖相关的操作，包括创建、查询、更新和删除
/// </summary>
public interface ISharePostService
{
    /// <summary>
    /// 分页获取分享帖子列表
    /// 业务意图：支持分页查询，按创建时间倒序排列，用于首页展示所有分享帖子
    /// </summary>
    /// <param name="request">分页请求参数</param>
    /// <returns>包含分页帖子列表的响应</returns>
    Task<ApiResponse> GetPagedAsync(PagedRequest request);

    /// <summary>
    /// 根据ID获取分享帖子详情
    /// 业务意图：获取单个帖子的完整信息，包括发布者信息和预订记录
    /// </summary>
    /// <param name="id">帖子ID</param>
    /// <returns>包含帖子详情的响应</returns>
    Task<ApiResponse> GetByIdAsync(int id);

    /// <summary>
    /// 创建分享帖子
    /// 业务意图：允许用户发布新的食物分享信息，自动关联发布者，设置初始状态为可领取
    /// </summary>
    /// <param name="userId">发布者用户ID</param>
    /// <param name="request">创建帖子请求参数</param>
    /// <returns>包含新创建帖子信息的响应</returns>
    Task<ApiResponse> CreateAsync(int userId, CreateSharePostRequest request);

    /// <summary>
    /// 更新分享帖子
    /// 业务意图：仅允许帖子所有者修改帖子信息，检查操作权限后执行更新
    /// </summary>
    /// <param name="id">帖子ID</param>
    /// <param name="userId">操作用户ID</param>
    /// <param name="request">更新帖子请求参数</param>
    /// <returns>包含更新后帖子信息的响应</returns>
    Task<ApiResponse> UpdateAsync(int id, int userId, UpdateSharePostRequest request);

    /// <summary>
    /// 删除分享帖子
    /// 业务意图：仅允许帖子所有者删除帖子，检查操作权限后执行删除
    /// </summary>
    /// <param name="id">帖子ID</param>
    /// <param name="userId">操作用户ID</param>
    /// <returns>操作结果响应</returns>
    Task<ApiResponse> DeleteAsync(int id, int userId);

    /// <summary>
    /// 更新分享帖子状态
    /// 业务意图：控制帖子状态流转（可领取→已预订→已取餐→已过期），检查状态转换的合法性和操作权限
    /// </summary>
    /// <param name="id">帖子ID</param>
    /// <param name="userId">操作用户ID</param>
    /// <param name="request">更新状态请求参数</param>
    /// <returns>包含更新后帖子信息的响应</returns>
    Task<ApiResponse> UpdateStatusAsync(int id, int userId, UpdateSharePostStatusRequest request);

    /// <summary>
    /// 从回收站恢复帖子
    /// 业务意图：恢复已软删除的帖子，检查权限（只能恢复自己删除的或自己拥有的帖子）
    /// </summary>
    /// <param name="id">帖子ID</param>
    /// <param name="userId">操作用户ID</param>
    /// <returns>操作结果响应</returns>
    Task<ApiResponse> RestoreAsync(int id, int userId);

    /// <summary>
    /// 查询回收站帖子列表
    /// 业务意图：获取当前用户删除的或拥有的已软删除帖子，支持分页
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="request">分页请求参数</param>
    /// <returns>包含回收站帖子列表的响应</returns>
    Task<ApiResponse> GetRecycleBinAsync(int userId, PagedRequest request);
}
