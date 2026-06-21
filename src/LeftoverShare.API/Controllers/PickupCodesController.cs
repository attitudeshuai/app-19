using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LeftoverShare.API.DTOs.Common;
using LeftoverShare.API.DTOs.PickupCodes;
using LeftoverShare.API.Entities.Enums;
using LeftoverShare.API.Helpers;
using LeftoverShare.API.Repositories;
using LeftoverShare.API.Services;

namespace LeftoverShare.API.Controllers;

/// <summary>
/// 取餐码控制器
/// 业务意图：处理取餐码的CRUD操作，使用 PostId（NOT SharePostId）、ClaimerId 和枚举状态
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PickupCodesController : ControllerBase
{
    private readonly IPickupCodeService _pickupCodeService;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    /// <summary>
    /// 构造函数：注入取餐码服务、当前用户接口、工作单元和映射器
    /// </summary>
    public PickupCodesController(IPickupCodeService pickupCodeService, ICurrentUser currentUser, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _pickupCodeService = pickupCodeService;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    /// <summary>
    /// 获取取餐码列表
    /// 业务意图：分页获取取餐码列表，需要登录
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetList([FromQuery] PagedRequest request)
    {
        // 调用服务获取分页数据
        var response = await _pickupCodeService.GetPagedAsync(request);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 创建取餐码
    /// 业务意图：为已确认的预约生成取餐码，使用 Reservation.PostId（NOT SharePostId）关联帖子，需要登录
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreatePickupCodeRequest request)
    {
        // 从 ICurrentUser 获取当前用户ID
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("用户未认证", 401));
        }

        // 调用服务创建取餐码，服务内部通过 Reservation.PostId 关联帖子
        var response = await _pickupCodeService.CreateAsync(userId.Value, request);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 获取取餐码详情
    /// 业务意图：根据ID获取取餐码的详细信息，需要登录
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetById(int id)
    {
        // 调用服务获取取餐码详情
        var response = await _pickupCodeService.GetByIdAsync(id);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 删除取餐码
    /// 业务意图：删除取餐码，需要登录
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

        // 调用服务删除取餐码
        var response = await _pickupCodeService.DeleteAsync(id, userId.Value);
        return StatusCode(response.Code, response);
    }
}
