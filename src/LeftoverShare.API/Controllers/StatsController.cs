using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LeftoverShare.API.Helpers;
using LeftoverShare.API.Services;

namespace LeftoverShare.API.Controllers;

/// <summary>
/// 统计控制器
/// 业务意图：提供系统统计数据接口，包括概览统计和趋势统计
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class StatsController : ControllerBase
{
    private readonly IStatsService _statsService;

    /// <summary>
    /// 构造函数：注入统计服务
    /// </summary>
    public StatsController(IStatsService statsService)
    {
        _statsService = statsService;
    }

    /// <summary>
    /// 获取总览统计
    /// 业务意图：获取系统的总体统计数据，包括用户总数、帖子总数、预约总数、积分总数，需要登录
    /// </summary>
    [HttpGet("overview")]
    [Authorize]
    public async Task<IActionResult> GetOverview()
    {
        // 调用服务获取概览统计数据
        var response = await _statsService.GetOverviewAsync();
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 获取趋势统计
    /// 业务意图：按时间范围获取帖子和预约的趋势统计数据，支持 ?startDate=&endDate= 查询参数，需要登录
    /// </summary>
    [HttpGet("trend")]
    [Authorize]
    public async Task<IActionResult> GetTrend([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        // 默认时间范围：最近30天
        var now = DateTime.UtcNow;
        var actualStartDate = startDate ?? now.AddDays(-30);
        var actualEndDate = endDate ?? now;

        // 确保开始日期不晚于结束日期
        if (actualStartDate > actualEndDate)
        {
            return BadRequest(ApiResponse.Fail("开始日期不能晚于结束日期"));
        }

        // 调用服务获取趋势统计数据
        var response = await _statsService.GetTrendAsync(actualStartDate, actualEndDate);
        return StatusCode(response.Code, response);
    }
}
