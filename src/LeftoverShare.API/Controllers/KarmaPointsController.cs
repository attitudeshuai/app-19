using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LeftoverShare.API.DTOs.Common;
using LeftoverShare.API.DTOs.KarmaPoints;
using LeftoverShare.API.Helpers;
using LeftoverShare.API.Repositories;
using LeftoverShare.API.Services;

namespace LeftoverShare.API.Controllers;

/// <summary>
/// 积分控制器
/// 业务意图：处理积分的CRUD操作，使用 RelatedId（NOT RelatedSharePostId）关联业务
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class KarmaPointsController : ControllerBase
{
    private readonly IKarmaPointService _karmaPointService;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    /// <summary>
    /// 构造函数：注入积分服务、当前用户接口、工作单元和映射器
    /// </summary>
    public KarmaPointsController(IKarmaPointService karmaPointService, ICurrentUser currentUser, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _karmaPointService = karmaPointService;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    /// <summary>
    /// 获取积分列表
    /// 业务意图：分页获取所有积分记录，需要登录
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetList([FromQuery] PagedRequest request)
    {
        // 调用服务获取分页数据
        var response = await _karmaPointService.GetPagedAsync(request);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 创建积分记录
    /// 业务意图：为用户添加积分记录，使用 RelatedId 关联业务实体，需要登录
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateKarmaPointRequest request)
    {
        // 调用服务创建积分记录，RelatedId 用于关联业务（NOT RelatedSharePostId）
        var response = await _karmaPointService.CreateAsync(request);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 获取积分详情
    /// 业务意图：根据ID获取积分记录的详细信息，需要登录
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetById(int id)
    {
        // 调用服务获取积分详情
        var response = await _karmaPointService.GetByIdAsync(id);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 更新积分记录
    /// 业务意图：更新积分记录信息，需要登录
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateKarmaPointRequest request)
    {
        // 调用服务更新积分记录
        var response = await _karmaPointService.UpdateAsync(id, request);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 删除积分记录
    /// 业务意图：删除积分记录，需要登录
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        // 调用服务删除积分记录
        var response = await _karmaPointService.DeleteAsync(id);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 获取我的积分记录
    /// 业务意图：获取当前登录用户的积分变动历史，使用 RelatedId（NOT RelatedSharePostId），需要登录
    /// </summary>
    [HttpGet("mine")]
    [Authorize]
    public async Task<IActionResult> GetMyKarmaPoints([FromQuery] PagedRequest request)
    {
        // 从 ICurrentUser 获取当前用户ID
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("用户未认证", 401));
        }

        // 调用服务获取当前用户的积分记录
        var response = await _karmaPointService.GetMineAsync(userId.Value, request);
        return StatusCode(response.Code, response);
    }
}
