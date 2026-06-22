using LeftoverShare.API.DTOs.Common;
using LeftoverShare.API.DTOs.RecycleBin;
using LeftoverShare.API.Helpers;

namespace LeftoverShare.API.Services;

/// <summary>
/// 回收站服务接口
/// 业务意图：定义回收站相关的操作，包括查询回收站、恢复已删除实体、查看快照、永久删除
/// </summary>
public interface IRecycleBinService
{
    /// <summary>
    /// 分页获取回收站列表
    /// 业务意图：支持按实体类型筛选，按删除时间倒序排列，用户只能看到自己删除或作为原所有者的记录
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="entityType">实体类型筛选（可选）：SharePost/Reservation/PickupCode/KarmaPoint</param>
    /// <param name="request">分页请求参数</param>
    /// <returns>包含分页回收站列表的响应</returns>
    Task<ApiResponse> GetRecycleBinAsync(int userId, string? entityType, PagedRequest request);

    /// <summary>
    /// 恢复已删除的实体
    /// 业务意图：根据快照信息恢复软删除的实体，仅允许原所有者或删除者执行恢复
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="request">恢复请求参数</param>
    /// <returns>操作结果响应</returns>
    Task<ApiResponse> RestoreAsync(int userId, RestoreRequest request);

    /// <summary>
    /// 获取快照详情
    /// 业务意图：查看已删除实体的完整快照数据，用于恢复前确认
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="snapshotId">快照ID</param>
    /// <returns>包含快照详情的响应</returns>
    Task<ApiResponse> GetSnapshotAsync(int userId, int snapshotId);

    /// <summary>
    /// 永久删除快照（仅供管理员使用）
    /// 业务意图：从回收站中彻底移除快照记录，无法再恢复
    /// </summary>
    /// <param name="userId">当前用户ID（需为管理员）</param>
    /// <param name="snapshotId">快照ID</param>
    /// <returns>操作结果响应</returns>
    Task<ApiResponse> PermanentlyDeleteAsync(int userId, int snapshotId);
}
