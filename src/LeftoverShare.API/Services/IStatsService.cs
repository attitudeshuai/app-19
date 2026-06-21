using LeftoverShare.API.DTOs.Stats;
using LeftoverShare.API.Helpers;

namespace LeftoverShare.API.Services;

/// <summary>
/// 统计服务接口
/// 业务意图：定义统计相关的操作，包括概览统计和趋势统计
/// </summary>
public interface IStatsService
{
    /// <summary>
    /// 获取概览统计数据
    /// 业务意图：获取平台核心指标的总览数据，包括用户总数、帖子总数、预订总数和积分总数
    /// </summary>
    /// <returns>包含概览统计数据的响应</returns>
    Task<ApiResponse> GetOverviewAsync();

    /// <summary>
    /// 获取趋势统计数据
    /// 业务意图：按日期统计帖子和预订数量的变化趋势，用于分析平台活跃度
    /// </summary>
    /// <param name="startDate">统计开始日期</param>
    /// <param name="endDate">统计结束日期</param>
    /// <returns>包含每日趋势统计数据的响应</returns>
    Task<ApiResponse> GetTrendAsync(DateTime startDate, DateTime endDate);
}
