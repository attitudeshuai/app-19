using LeftoverShare.API.DTOs.Common;
using LeftoverShare.API.DTOs.KarmaPoints;
using LeftoverShare.API.Helpers;

namespace LeftoverShare.API.Services;

/// <summary>
/// 积分服务接口
/// 业务意图：定义积分相关的操作，包括创建、查询和统计
/// </summary>
public interface IKarmaPointService
{
    /// <summary>
    /// 分页获取所有积分记录
    /// 业务意图：用于管理后台展示所有积分交易记录，支持分页查询
    /// </summary>
    /// <param name="request">分页请求参数</param>
    /// <returns>包含分页积分记录列表的响应</returns>
    Task<ApiResponse> GetPagedAsync(PagedRequest request);

    /// <summary>
    /// 根据ID获取积分记录详情
    /// 业务意图：获取单条积分记录的详细信息，用于审计和问题排查
    /// </summary>
    /// <param name="id">积分记录ID</param>
    /// <returns>包含积分记录详情的响应</returns>
    Task<ApiResponse> GetByIdAsync(int id);

    /// <summary>
    /// 获取当前用户的积分记录
    /// 业务意图：用户查看自己的积分变动历史，支持分页展示
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="request">分页请求参数</param>
    /// <returns>包含用户积分记录列表的响应</returns>
    Task<ApiResponse> GetMineAsync(int userId, PagedRequest request);

    /// <summary>
    /// 创建积分记录
    /// 业务意图：增加或扣减用户积分，记录积分变动原因和关联业务ID，同时更新用户总积分
    /// </summary>
    /// <param name="request">创建积分记录请求参数</param>
    /// <returns>包含新创建积分记录信息的响应</returns>
    Task<ApiResponse> CreateAsync(CreateKarmaPointRequest request);

    /// <summary>
    /// 更新积分记录
    /// 业务意图：修正积分记录，通常用于管理后台纠正错误的积分发放，同时更新用户总积分
    /// </summary>
    /// <param name="id">积分记录ID</param>
    /// <param name="request">更新积分记录请求参数</param>
    /// <returns>包含更新后积分记录信息的响应</returns>
    Task<ApiResponse> UpdateAsync(int id, UpdateKarmaPointRequest request);

    /// <summary>
    /// 删除积分记录
    /// 业务意图：删除错误的积分记录，同时回滚用户的总积分
    /// </summary>
    /// <param name="id">积分记录ID</param>
    /// <returns>操作结果响应</returns>
    Task<ApiResponse> DeleteAsync(int id);

    /// <summary>
    /// 从回收站恢复积分记录
    /// 业务意图：恢复已软删除的积分记录，检查权限（只能恢复自己删除的或自己拥有的积分记录），同时恢复用户总积分
    /// </summary>
    /// <param name="id">积分记录ID</param>
    /// <param name="userId">操作用户ID</param>
    /// <returns>操作结果响应</returns>
    Task<ApiResponse> RestoreAsync(int id, int userId);

    /// <summary>
    /// 查询回收站积分记录列表
    /// 业务意图：获取当前用户相关的已软删除积分记录，支持分页
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="request">分页请求参数</param>
    /// <returns>包含回收站积分记录列表的响应</returns>
    Task<ApiResponse> GetRecycleBinAsync(int userId, PagedRequest request);
}
