using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LeftoverShare.API.DTOs.Common;
using LeftoverShare.API.DTOs.Reservations;
using LeftoverShare.API.Entities.Enums;
using LeftoverShare.API.Helpers;
using LeftoverShare.API.Services;

namespace LeftoverShare.API.Controllers;

/// <summary>
/// 预约控制器
/// 业务意图：处理预约的CRUD操作，所有操作使用枚举状态（ReservationStatus），NOT 字符串
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservationService;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;

    /// <summary>
    /// 构造函数：注入预约服务、当前用户接口和映射器
    /// </summary>
    public ReservationsController(IReservationService reservationService, ICurrentUser currentUser, IMapper mapper)
    {
        _reservationService = reservationService;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    /// <summary>
    /// 获取预约列表
    /// 业务意图：分页获取预约列表，支持 ?mine=true 查看我的预约，需要登录
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetList([FromQuery] PagedRequest request, [FromQuery] bool mine = false)
    {
        // 从 ICurrentUser 获取当前用户ID
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("用户未认证", 401));
        }

        // 根据 mine 参数决定是否只查看当前用户的预约
        var response = await _reservationService.GetPagedAsync(request, mine ? userId : null);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 创建预约
    /// 业务意图：用户预约分享帖，状态使用枚举 ReservationStatus，需要登录
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateReservationRequest request)
    {
        // 从 ICurrentUser 获取当前用户ID
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("用户未认证", 401));
        }

        // 调用服务创建预约，服务内部使用枚举状态 ReservationStatus.Pending
        var response = await _reservationService.CreateAsync(userId.Value, request);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 获取预约详情
    /// 业务意图：根据ID获取预约的详细信息，需要登录
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetById(int id)
    {
        // 调用服务获取预约详情
        var response = await _reservationService.GetByIdAsync(id);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 更新预约
    /// 业务意图：更新预约信息，需要登录
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateReservationRequest request)
    {
        // 从 ICurrentUser 获取当前用户ID
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("用户未认证", 401));
        }

        // 调用服务更新预约
        var response = await _reservationService.UpdateAsync(id, userId.Value, request);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 删除预约
    /// 业务意图：删除预约，需要登录
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

        // 调用服务删除预约
        var response = await _reservationService.DeleteAsync(id, userId.Value);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 修改预约状态
    /// 业务意图：修改预约状态，使用枚举 ReservationStatus（Pending/Confirmed/Completed/Cancelled），NOT 字符串，需要登录
    /// </summary>
    [HttpPatch("{id}/status")]
    [Authorize]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateReservationStatusRequest request)
    {
        // 从 ICurrentUser 获取当前用户ID
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("用户未认证", 401));
        }

        // 验证状态值是否为有效的枚举
        if (!Enum.TryParse<ReservationStatus>(request.Status, true, out var status))
        {
            return BadRequest(ApiResponse.Fail("无效的预约状态值，请使用枚举值：Pending/Confirmed/Completed/Cancelled"));
        }

        // 调用服务更新预约状态，传入枚举状态
        var response = await _reservationService.UpdateStatusAsync(id, userId.Value, request);
        return StatusCode(response.Code, response);
    }
}
