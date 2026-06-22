using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LeftoverShare.API.DTOs.Common;
using LeftoverShare.API.DTOs.RecycleBin;
using LeftoverShare.API.Helpers;
using LeftoverShare.API.Services;

namespace LeftoverShare.API.Controllers;

/// <summary>
/// 回收站控制器
/// 业务意图：提供回收站查询、恢复已删除实体、查看快照详情、永久删除等接口
/// </summary>
[ApiController]
[Route("api/recyclebin")]
[Authorize]
public class RecycleBinController : ControllerBase
{
    private readonly IRecycleBinService _recycleBinService;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// 构造函数：注入回收站服务和当前用户接口
    /// </summary>
    public RecycleBinController(IRecycleBinService recycleBinService, ICurrentUser currentUser)
    {
        _recycleBinService = recycleBinService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 查询回收站列表
    /// 业务意图：分页查询回收站中的软删除记录，支持按实体类型筛选
    /// 用户只能看到自己删除或作为原所有者的记录
    /// </summary>
    /// <param name="entityType">实体类型筛选（可选）：SharePost/Reservation/PickupCode/KarmaPoint</param>
    /// <param name="pageNumber">页码（默认1）</param>
    /// <param name="pageSize">每页数量（默认10）</param>
    /// <returns>分页的回收站列表</returns>
    [HttpGet]
    public async Task<IActionResult> GetRecycleBin(
        [FromQuery] string? entityType = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("用户未认证", 401));
        }

        var request = new PagedRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var response = await _recycleBinService.GetRecycleBinAsync(userId.Value, entityType, request);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 恢复已删除的实体
    /// 业务意图：根据快照信息恢复软删除的实体，仅允许原所有者或删除者执行恢复
    /// </summary>
    /// <param name="request">恢复请求，包含快照ID和实体类型</param>
    /// <returns>恢复操作结果</returns>
    [HttpPost("restore")]
    public async Task<IActionResult> Restore([FromBody] RestoreRequest request)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("用户未认证", 401));
        }

        var response = await _recycleBinService.RestoreAsync(userId.Value, request);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 获取快照详情
    /// 业务意图：查看已删除实体的完整快照数据，用于恢复前确认
    /// </summary>
    /// <param name="id">快照ID</param>
    /// <returns>快照详情</returns>
    [HttpGet("snapshots/{id:int}")]
    public async Task<IActionResult> GetSnapshot(int id)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("用户未认证", 401));
        }

        var response = await _recycleBinService.GetSnapshotAsync(userId.Value, id);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 永久删除快照（物理删除）
    /// 业务意图：从回收站中彻底移除快照记录，无法再恢复，仅供管理员使用
    /// </summary>
    /// <param name="id">快照ID</param>
    /// <returns>删除操作结果</returns>
    [HttpDelete("snapshots/{id:int}")]
    public async Task<IActionResult> PermanentlyDelete(int id)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("用户未认证", 401));
        }

        var response = await _recycleBinService.PermanentlyDeleteAsync(userId.Value, id);
        return StatusCode(response.Code, response);
    }
}
