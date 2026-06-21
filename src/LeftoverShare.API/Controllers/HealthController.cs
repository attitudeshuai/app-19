using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LeftoverShare.API.Helpers;

namespace LeftoverShare.API.Controllers;

/// <summary>
/// 健康检查控制器
/// 业务意图：提供系统健康检查接口，匿名可访问，返回 ApiResponse 格式
/// </summary>
[ApiController]
[Route("health")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    /// <summary>
    /// 健康检查
    /// 业务意图：检查系统运行状态，匿名可访问，返回统一 ApiResponse 格式
    /// </summary>
    [HttpGet]
    public IActionResult Check()
    {
        // 返回健康状态，使用统一 ApiResponse 格式
        return Ok(ApiResponse.Success(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow
        }, "系统运行正常"));
    }
}
