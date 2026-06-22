using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LeftoverShare.API.Helpers;
using LeftoverShare.API.Services;
using LeftoverShare.API.DTOs.AllergenTags;

namespace LeftoverShare.API.Controllers;

/// <summary>
/// 过敏原标签控制器
/// 业务意图：管理过敏原标签的增删改查
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AllergenTagsController : ControllerBase
{
    private readonly IAllergenTagService _allergenTagService;
    private readonly ICurrentUser _currentUser;

    public AllergenTagsController(IAllergenTagService allergenTagService, ICurrentUser currentUser)
    {
        _allergenTagService = allergenTagService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 获取所有启用的标签（公开）
    /// </summary>
    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllActive()
    {
        var response = await _allergenTagService.GetAllActiveAsync();
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 分页获取标签列表（仅管理员）
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] bool includeInactive = false, [FromQuery] int? severityLevel = null, [FromQuery] string? searchTerm = null)
    {
        var response = await _allergenTagService.GetPagedAsync(pageNumber, pageSize, includeInactive, severityLevel, searchTerm);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 获取标签详情（公开）
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var response = await _allergenTagService.GetByIdAsync(id);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 创建标签（仅管理员）
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateAllergenTagRequest request)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("用户未认证", 401));
        }
        var response = await _allergenTagService.CreateAsync(userId.Value, request);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 更新标签（仅管理员）
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAllergenTagRequest request)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("用户未认证", 401));
        }
        var response = await _allergenTagService.UpdateAsync(id, userId.Value, request);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 删除标签（仅管理员，软删除）
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("用户未认证", 401));
        }
        var response = await _allergenTagService.DeleteAsync(id, userId.Value);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 启用/禁用标签（仅管理员）
    /// </summary>
    [HttpPatch("{id}/toggle-active")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleActive(int id, [FromBody] bool isActive)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("用户未认证", 401));
        }
        var response = await _allergenTagService.ToggleActiveAsync(id, userId.Value, isActive);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 批量更新排序（仅管理员）
    /// </summary>
    [HttpPatch("sort-order")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateSortOrder([FromBody] Dictionary<int, int> sortOrders)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("用户未认证", 401));
        }
        var response = await _allergenTagService.UpdateSortOrderAsync(userId.Value, sortOrders);
        return StatusCode(response.Code, response);
    }
}
