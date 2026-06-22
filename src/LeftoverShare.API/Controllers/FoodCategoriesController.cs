using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LeftoverShare.API.Helpers;
using LeftoverShare.API.Services;
using LeftoverShare.API.DTOs.FoodCategories;

namespace LeftoverShare.API.Controllers;

/// <summary>
/// 食物分类控制器
/// 业务意图：管理食物分类的增删改查，树形结构展示
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FoodCategoriesController : ControllerBase
{
    private readonly IFoodCategoryService _foodCategoryService;
    private readonly ICurrentUser _currentUser;

    public FoodCategoriesController(IFoodCategoryService foodCategoryService, ICurrentUser currentUser)
    {
        _foodCategoryService = foodCategoryService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 获取分类树（公开）
    /// </summary>
    [HttpGet("tree")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTree([FromQuery] bool includeInactive = false)
    {
        var response = await _foodCategoryService.GetTreeAsync(includeInactive);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 获取顶级分类列表（公开）
    /// </summary>
    [HttpGet("roots")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRootCategories([FromQuery] bool includeInactive = false)
    {
        var response = await _foodCategoryService.GetRootCategoriesAsync(includeInactive);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 获取子分类列表（公开）
    /// </summary>
    [HttpGet("{parentId}/children")]
    [AllowAnonymous]
    public async Task<IActionResult> GetChildren(int parentId, [FromQuery] bool includeInactive = false)
    {
        var response = await _foodCategoryService.GetChildrenAsync(parentId, includeInactive);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 获取分类详情（公开）
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var response = await _foodCategoryService.GetByIdAsync(id);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 创建分类（管理员）
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateFoodCategoryRequest request)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("用户未认证", 401));
        }
        var response = await _foodCategoryService.CreateAsync(userId.Value, request);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 更新分类（管理员）
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateFoodCategoryRequest request)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("用户未认证", 401));
        }
        var response = await _foodCategoryService.UpdateAsync(id, userId.Value, request);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 删除分类（管理员，软删除
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
        var response = await _foodCategoryService.DeleteAsync(id, userId.Value);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 启用/禁用分类（管理员）
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
        var response = await _foodCategoryService.ToggleActiveAsync(id, userId.Value, isActive);
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
        var response = await _foodCategoryService.UpdateSortOrderAsync(userId.Value, sortOrders);
        return StatusCode(response.Code, response);
    }
}
