using System.Net;
using System.Text.Json;
using FluentValidation;
using LeftoverShare.API.Helpers;
using LeftoverShare.API.Helpers.Exceptions;
using Microsoft.EntityFrameworkCore;

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
        int? businessErrorCode = null;
        IDictionary<string, object>? errorDetails = null;

        switch (exception)
        {
            // 预约业务异常：返回 409 Conflict 或 400 Bad Request
            case ReservationException reservationException:
                statusCode = GetHttpStatusCodeForReservationError(reservationException.ErrorCode);
                message = reservationException.Message;
                businessErrorCode = (int)reservationException.ErrorCode;
                errorDetails = reservationException.Details;
                break;

            // EF Core 并发冲突异常
            case DbUpdateConcurrencyException concurrencyException:
                statusCode = HttpStatusCode.Conflict;
                message = "数据已被其他用户修改，请刷新后重试";
                businessErrorCode = 4001;
                errorDetails = new Dictionary<string, object>
                {
                    ["canRetry"] = true,
                    ["suggestion"] = "请刷新页面后重试"
                };
                _logger.LogWarning(concurrencyException, "检测到数据库并发冲突");
                break;

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
        var response = ApiResponse.Fail(
            message: message,
            code: (int)statusCode,
            errorCode: businessErrorCode,
            errorDetails: errorDetails);

        // 序列化响应并写入
        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return context.Response.WriteAsync(jsonResponse);
    }

    /// <summary>
    /// 根据预约错误码获取对应的 HTTP 状态码
    /// </summary>
    private static HttpStatusCode GetHttpStatusCodeForReservationError(LeftoverShare.API.Entities.Enums.ReservationErrorCode errorCode)
    {
        return errorCode switch
        {
            LeftoverShare.API.Entities.Enums.ReservationErrorCode.PostNotFound => HttpStatusCode.NotFound,
            LeftoverShare.API.Entities.Enums.ReservationErrorCode.ReservationNotFound => HttpStatusCode.NotFound,
            LeftoverShare.API.Entities.Enums.ReservationErrorCode.InsufficientStock => HttpStatusCode.Conflict,
            LeftoverShare.API.Entities.Enums.ReservationErrorCode.QuantityExceedsAvailable => HttpStatusCode.Conflict,
            LeftoverShare.API.Entities.Enums.ReservationErrorCode.DuplicateReservation => HttpStatusCode.Conflict,
            LeftoverShare.API.Entities.Enums.ReservationErrorCode.ConcurrencyConflict => HttpStatusCode.Conflict,
            LeftoverShare.API.Entities.Enums.ReservationErrorCode.ReservationTimeout => HttpStatusCode.ServiceUnavailable,
            LeftoverShare.API.Entities.Enums.ReservationErrorCode.SystemBusy => HttpStatusCode.ServiceUnavailable,
            LeftoverShare.API.Entities.Enums.ReservationErrorCode.PermissionDenied => HttpStatusCode.Forbidden,
            LeftoverShare.API.Entities.Enums.ReservationErrorCode.InvalidStatusTransition => HttpStatusCode.Conflict,
            _ => HttpStatusCode.BadRequest
        };
    }
}
