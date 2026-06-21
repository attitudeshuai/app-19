using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LeftoverShare.API.DTOs.Auth;
using LeftoverShare.API.Helpers;
using LeftoverShare.API.Services;

namespace LeftoverShare.API.Controllers;

/// <summary>
/// 认证控制器
/// 业务意图：处理用户注册、登录、个人信息管理等认证相关操作
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;

    /// <summary>
    /// 构造函数：注入认证服务、当前用户接口和映射器
    /// </summary>
    public AuthController(IAuthService authService, ICurrentUser currentUser, IMapper mapper)
    {
        _authService = authService;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    /// <summary>
    /// 用户注册
    /// 业务意图：创建新用户账户，匿名可访问
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // 调用认证服务完成注册
        var response = await _authService.RegisterAsync(request);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 用户登录
    /// 业务意图：验证用户凭据并返回令牌，匿名可访问
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // 调用认证服务完成登录
        var response = await _authService.LoginAsync(request);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 获取当前用户信息
    /// 业务意图：根据当前登录用户ID获取详细信息
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        // 从 ICurrentUser 获取当前用户ID
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("用户未认证", 401));
        }

        // 调用认证服务获取用户信息
        var response = await _authService.GetCurrentUserAsync(userId.Value);
        return StatusCode(response.Code, response);
    }

    /// <summary>
    /// 更新个人信息
    /// 业务意图：更新当前登录用户的个人资料
    /// </summary>
    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        // 从 ICurrentUser 获取当前用户ID
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("用户未认证", 401));
        }

        // 调用认证服务更新用户资料
        var response = await _authService.UpdateProfileAsync(userId.Value, request);
        return StatusCode(response.Code, response);
    }
}
