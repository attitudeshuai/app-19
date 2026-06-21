namespace LeftoverShare.API.Helpers;

/// <summary>
/// 当前用户实现类
/// 业务意图：从 HTTP 上下文中获取当前登录用户 ID，实现 ICurrentUser 接口
/// </summary>
public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// 初始化当前用户实现类
    /// 业务意图：注入 HTTP 上下文访问器，用于获取 HTTP 请求上下文
    /// </summary>
    /// <param name="httpContextAccessor">HTTP 上下文访问器</param>
    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// 获取当前登录用户 ID
    /// 业务意图：从 HttpContext.Items 中获取由中间件存储的用户 ID
    /// </summary>
    public int? UserId
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;

            if (context == null)
            {
                return null;
            }

            if (context.Items.TryGetValue("UserId", out var userIdObj) && userIdObj is int userId)
            {
                return userId;
            }

            return null;
        }
    }
}
