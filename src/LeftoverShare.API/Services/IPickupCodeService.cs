using LeftoverShare.API.DTOs.Common;
using LeftoverShare.API.DTOs.PickupCodes;
using LeftoverShare.API.Helpers;

namespace LeftoverShare.API.Services;

/// <summary>
/// 取餐码服务接口
/// 业务意图：定义取餐码相关的操作，包括生成、验证和查询
/// </summary>
public interface IPickupCodeService
{
    /// <summary>
    /// 分页获取取餐码列表
    /// 业务意图：用于管理后台展示所有取餐码记录，支持分页查询
    /// </summary>
    /// <param name="request">分页请求参数</param>
    /// <returns>包含分页取餐码列表的响应</returns>
    Task<ApiResponse> GetPagedAsync(PagedRequest request);

    /// <summary>
    /// 根据ID获取取餐码详情
    /// 业务意图：获取单个取餐码的详细信息，包括使用状态和过期时间
    /// </summary>
    /// <param name="id">取餐码ID</param>
    /// <returns>包含取餐码详情的响应</returns>
    Task<ApiResponse> GetByIdAsync(int id);

    /// <summary>
    /// 创建取餐码
    /// 业务意图：为预订生成随机6位取餐码，设置过期时间（默认24小时）
    /// </summary>
    /// <param name="userId">操作用户ID</param>
    /// <param name="request">创建取餐码请求参数</param>
    /// <returns>包含新创建取餐码信息的响应</returns>
    Task<ApiResponse> CreateAsync(int userId, CreatePickupCodeRequest request);

    /// <summary>
    /// 删除取餐码
    /// 业务意图：仅允许相关人员删除取餐码，通常用于取消预订时清理
    /// </summary>
    /// <param name="id">取餐码ID</param>
    /// <param name="userId">操作用户ID</param>
    /// <returns>操作结果响应</returns>
    Task<ApiResponse> DeleteAsync(int id, int userId);
}
