using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LeftoverShare.API.DTOs.Common;
using LeftoverShare.API.DTOs.SharePosts;
using LeftoverShare.API.Helpers;
using LeftoverShare.API.Services;

namespace LeftoverShare.API.Controllers;

/// <summary>
/// 分享帖控制器
/// 业务意图：处理分享帖的CRUD操作，包括列表查询、创建、详情、更新、删除和状态修改
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SharePostsController : ControllerBase
{
    private readonly ISharePostService _sharePostService;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// 构造函数：注入分享帖服务和当前用户接口
    /// </summary>
    public SharePostsController(ISharePostService sharePostService, ICurrentUser currentUser)
    {
        _sharePostService = sharePostService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 获取分享帖列表
    /// 业务意图：分页获取分享帖列表，匿名可访问
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] PagedRequest request)
    {
        // 调用服务获取分页数据
        var response = await _sharePostService.GetPagedAsync(request);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 创建分享帖
    /// 业务意图：用户发布新的食物分享帖，需要登录
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateSharePostRequest request)
    {
        // 从 ICurrentUser 获取当前用户ID
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("用户未认证", 401));
        }

        // 调用服务创建分享帖
        var response = await _sharePostService.CreateAsync(userId.Value, request);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 获取分享帖详情
    /// 业务意图：根据ID获取分享帖的详细信息，匿名可访问
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        // 调用服务获取帖子详情
        var response = await _sharePostService.GetByIdAsync(id);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 更新分享帖
    /// 业务意图：更新分享帖信息，仅帖主可操作，需要登录
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSharePostRequest request)
    {
        // 从 ICurrentUser 获取当前用户ID
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("用户未认证", 401));
        }

        // 调用服务更新分享帖
        var response = await _sharePostService.UpdateAsync(id, userId.Value, request);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 删除分享帖
    /// 业务意图：删除分享帖，仅帖主可操作，需要登录
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        // 从 ICurrentUser 获取当前用户ID
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("用户未认证", 401));
        }

        // 调用服务删除分享帖
        var response = await _sharePostService.DeleteAsync(id, userId.Value);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 修改分享帖状态
    /// 业务意图：修改分享帖的状态（如可领取、已预订、已取餐、已过期），仅帖主可操作，需要登录
    /// </summary>
    [HttpPatch("{id}/status")]
    [Authorize]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateSharePostStatusRequest request)
    {
        // 从 ICurrentUser 获取当前用户ID
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("用户未认证", 401));
        }

        // 调用服务更新分享帖状态
        var response = await _sharePostService.UpdateStatusAsync(id, userId.Value, request);
        return StatusCode(response.Code, response);
    }
}
