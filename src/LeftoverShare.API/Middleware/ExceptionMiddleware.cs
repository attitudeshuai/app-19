using LeftoverShare.API.Helpers;
using System.Text.Json;

namespace LeftoverShare.API.Middleware;

/// <summary>
/// 全局异常处理中间件
/// 业务意图：统一捕获和处理应用程序中的所有异常，提供一致的错误响应格式
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    /// <summary>
    /// 初始化异常处理中间件
    /// 业务意图：注入必要的依赖项，包括请求委托、日志记录器和环境信息
    /// </summary>
    /// <param name="next">下一个中间件的请求委托</param>
    /// <param name="logger">日志记录器，用于记录异常信息</param>
    /// <param name="env">宿主环境信息，用于区分开发/生产环境</param>
    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    /// <summary>
    /// 执行中间件逻辑
    /// 业务意图：尝试执行后续中间件，捕获任何未处理的异常并进行统一处理
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    /// <returns>表示异步操作的任务</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// 处理异常并返回统一格式的响应
    /// 业务意图：记录异常日志，根据环境返回不同详细程度的错误信息
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    /// <param name="ex">捕获到的异常</param>
    /// <returns>表示异步操作的任务</returns>
    private Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        _logger.LogError(ex, "发生未处理的异常: {Message}", ex.Message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var response = _env.IsDevelopment()
            ? ApiResponse.Fail($"服务器内部错误: {ex.Message} {ex.StackTrace}", 500)
            : ApiResponse.Fail("服务器内部错误，请稍后重试", 500);

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(response, options);

        return context.Response.WriteAsync(json);
    }
}
