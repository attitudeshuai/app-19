using System.Security.Claims;

namespace LeftoverShare.API.Middleware;

/// <summary>
/// 当前用户中间件
/// 业务意图：从 JWT Token 中解析用户 ID 并存储到 HTTP 上下文中，供后续业务逻辑使用
/// </summary>
public class CurrentUserMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// 初始化当前用户中间件
    /// 业务意图：注入下一个中间件的请求委托
    /// </summary>
    /// <param name="next">下一个中间件的请求委托</param>
    public CurrentUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// 执行中间件逻辑
    /// 业务意图：从 JWT Token 中提取用户 ID，存储到 HttpContext.Items 中
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    /// <returns>表示异步操作的任务</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var userId = GetUserIdFromToken(context);

        if (userId.HasValue)
        {
            context.Items["UserId"] = userId.Value;
        }

        await _next(context);
    }

    /// <summary>
    /// 从 JWT Token 中提取用户 ID
    /// 业务意图：解析 ClaimsPrincipal 中的用户标识，获取用户 ID
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    /// <returns>用户 ID，如果未找到则返回 null</returns>
    private static int? GetUserIdFromToken(HttpContext context)
    {
        var claimsIdentity = context.User.Identity as ClaimsIdentity;

        if (claimsIdentity == null)
        {
            return null;
        }

        var userIdClaim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier) ?? claimsIdentity.FindFirst("UserId");

        if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
        {
            return null;
        }

        if (int.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }

        return null;
    }
}
