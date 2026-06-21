using System.Net;
using System.Text.Json;
using FluentValidation;
using LeftoverShare.API.Helpers;

namespace LeftoverShare.API.Middleware;

/// <summary>
/// 异常处理中间件
/// 业务意图：全局捕获并处理应用程序中的异常，返回统一格式的 ApiResponse（NOT ApiResponse<>）
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    /// <summary>
    /// 构造函数：注入请求委托和日志记录器
    /// </summary>
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// 执行中间件逻辑
    /// 业务意图：捕获请求处理过程中的异常并进行统一处理
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // 捕获异常后统一处理
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// 处理异常并返回统一 ApiResponse 响应
    /// 业务意图：根据异常类型记录日志并返回相应的 ApiResponse 格式（非泛型），NOT ApiResponse<>
    /// </summary>
    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // 记录异常日志
        _logger.LogError(exception, "处理请求时发生未处理的异常: {Message}", exception.Message);

        // 设置响应内容类型
        context.Response.ContentType = "application/json";

        // 根据异常类型设置状态码和错误信息
        var statusCode = HttpStatusCode.InternalServerError;
        var message = "服务器内部错误";

        switch (exception)
        {
            // 验证异常：返回 400 Bad Request
            case ValidationException validationException:
                statusCode = HttpStatusCode.BadRequest;
                message = string.Join("; ", validationException.Errors.Select(e => e.ErrorMessage));
                break;

            // 未找到异常：返回 404 Not Found
            case KeyNotFoundException:
                statusCode = HttpStatusCode.NotFound;
                message = exception.Message;
                break;

            // 未授权异常：返回 401 Unauthorized
            case UnauthorizedAccessException:
                statusCode = HttpStatusCode.Unauthorized;
                message = exception.Message;
                break;

            // 应用程序异常：返回 400 Bad Request
            case ApplicationException:
                statusCode = HttpStatusCode.BadRequest;
                message = exception.Message;
                break;

            // 其他异常：返回 500 Internal Server Error
            default:
                message = exception.Message;
                break;
        }

        // 设置响应状态码
        context.Response.StatusCode = (int)statusCode;

        // 使用非泛型 ApiResponse（NOT ApiResponse<>）返回统一格式
        var response = ApiResponse.Fail(message, (int)statusCode);

        // 序列化响应并写入
        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return context.Response.WriteAsync(jsonResponse);
    }
}
