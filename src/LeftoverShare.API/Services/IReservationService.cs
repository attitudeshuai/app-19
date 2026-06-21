using LeftoverShare.API.DTOs.Common;
using LeftoverShare.API.DTOs.Reservations;
using LeftoverShare.API.Helpers;

namespace LeftoverShare.API.Services;

/// <summary>
/// 预约服务接口
/// 业务意图：定义预约相关的操作，包括创建、查询、更新和取消
/// </summary>
public interface IReservationService
{
    /// <summary>
    /// 分页获取预订列表
    /// 业务意图：支持按用户筛选预订记录，用于管理后台或用户中心展示
    /// </summary>
    /// <param name="request">分页请求参数</param>
    /// <param name="userId">可选的用户筛选ID</param>
    /// <returns>包含分页预订列表的响应</returns>
    Task<ApiResponse> GetPagedAsync(PagedRequest request, int? userId = null);

    /// <summary>
    /// 根据ID获取预订详情
    /// 业务意图：获取单个预订的完整信息，包括关联的帖子和用户信息
    /// </summary>
    /// <param name="id">预订ID</param>
    /// <returns>包含预订详情的响应</returns>
    Task<ApiResponse> GetByIdAsync(int id);

    /// <summary>
    /// 创建预订
    /// 业务意图：用户预订食物，检查帖子状态是否可预订，自动生成取餐码
    /// </summary>
    /// <param name="userId">预订用户ID</param>
    /// <param name="request">创建预订请求参数</param>
    /// <returns>包含新创建预订信息的响应</returns>
    Task<ApiResponse> CreateAsync(int userId, CreateReservationRequest request);

    /// <summary>
    /// 更新预订信息
    /// 业务意图：仅允许预订者或帖子发布者修改预订备注等信息
    /// </summary>
    /// <param name="id">预订ID</param>
    /// <param name="userId">操作用户ID</param>
    /// <param name="request">更新预订请求参数</param>
    /// <returns>包含更新后预订信息的响应</returns>
    Task<ApiResponse> UpdateAsync(int id, int userId, UpdateReservationRequest request);

    /// <summary>
    /// 取消预订
    /// 业务意图：仅允许预订者或帖子发布者取消预订，检查状态流转合法性
    /// </summary>
    /// <param name="id">预订ID</param>
    /// <param name="userId">操作用户ID</param>
    /// <returns>操作结果响应</returns>
    Task<ApiResponse> DeleteAsync(int id, int userId);

    /// <summary>
    /// 更新预订状态
    /// 业务意图：控制预订状态流转（待确认→已确认→已完成/已取消），完成时自动发放积分奖励
    /// </summary>
    /// <param name="id">预订ID</param>
    /// <param name="userId">操作用户ID</param>
    /// <param name="request">更新状态请求参数</param>
    /// <returns>包含更新后预订信息的响应</returns>
    Task<ApiResponse> UpdateStatusAsync(int id, int userId, UpdateReservationStatusRequest request);
}
