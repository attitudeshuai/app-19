using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LeftoverShare.API.Helpers;
using LeftoverShare.API.Services;
using LeftoverShare.API.DTOs.PostTags;

namespace LeftoverShare.API.Controllers;

/// <summary>
/// 帖子标签控制器
/// 业务意图：管理帖子标签（系统预设+用户自定义）
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PostTagsController : ControllerBase
{
    private readonly IPostTagService _postTagService;
    private readonly ICurrentUser _currentUser;

    public PostTagsController(IPostTagService postTagService, ICurrentUser currentUser)
    {
        _postTagService = postTagService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 获取所有启用的标签（公开）
    /// </summary>
    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllActive([FromQuery] bool includeUserDefined = true)
    {
        var response = await _postTagService.GetAllActiveAsync(includeUserDefined);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 获取热门标签（公开）
    /// </summary>
    [HttpGet("popular")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPopular([FromQuery] int topN = 20)
    {
        var response = await _postTagService.GetPopularAsync(topN);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 搜索标签（公开，用于自动补全）
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> Search([FromQuery] string keyword, [FromQuery] int limit = 20)
    {
        var response = await _postTagService.SearchAsync(keyword, limit);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 分页获取标签列表（管理员）
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] bool includeInactive = false, [FromQuery] bool? isSystemDefined = null, [FromQuery] int? createdBy = null, [FromQuery] string? searchTerm = null)
    {
        var response = await _postTagService.GetPagedAsync(pageNumber, pageSize, includeInactive, isSystemDefined, createdBy, searchTerm);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 获取我创建的标签（登录用户）
    /// </summary>
    [HttpGet("mine")]
    [Authorize]
    public async Task<IActionResult> GetMyTags()
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("用户未认证", 401));
        }
        var response = await _postTagService.GetMyTagsAsync(userId.Value);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 获取标签详情（公开）
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var response = await _postTagService.GetByIdAsync(id);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 创建系统预设标签（管理员）
    /// </summary>
    [HttpPost("system")]
    [Authorize]
    public async Task<IActionResult> CreateSystemTag([FromBody] CreatePostTagRequest request)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("用户未认证", 401));
        }
        var response = await _postTagService.CreateSystemTagAsync(userId.Value, request);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 用户创建自定义标签（登录用户）
    /// </summary>
    [HttpPost("user")]
    [Authorize]
    public async Task<IActionResult> CreateUserTag([FromBody] CreatePostTagRequest request)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("用户未认证", 401));
        }
        var response = await _postTagService.CreateUserTagAsync(userId.Value, request);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 更新标签（管理员或创建者）
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePostTagRequest request)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("用户未认证", 401));
        }
        var response = await _postTagService.UpdateAsync(id, userId.Value, request);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 删除标签（管理员或创建者，软删除）
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("用户未认证", 401));
        }
        var response = await _postTagService.DeleteAsync(id, userId.Value);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 启用/禁用标签（管理员）
    /// </summary>
    [HttpPatch("{id}/toggle-active")]
    [Authorize]
    public async Task<IActionResult> ToggleActive(int id, [FromBody] bool isActive)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("用户未认证", 401));
        }
        var response = await _postTagService.ToggleActiveAsync(id, userId.Value, isActive);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 批量更新排序（管理员）
    /// </summary>
    [HttpPatch("sort-order")]
    [Authorize]
    public async Task<IActionResult> UpdateSortOrder([FromBody] Dictionary<int, int> sortOrders)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("用户未认证", 401));
        }
        var response = await _postTagService.UpdateSortOrderAsync(userId.Value, sortOrders);
        return StatusCode(response.Code, response);
    }
}
