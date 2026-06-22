namespace LeftoverShare.API.Helpers;

/// <summary>
/// 统一 API 返回格式
/// 业务意图：规范所有 API 接口的响应格式，便于前端统一处理响应数据
/// </summary>
public class ApiResponse
{
    /// <summary>
    /// 状态码：表示请求的处理结果
    /// 业务意图：200 表示成功，其他代码表示不同类型的错误
    /// </summary>
    public int Code { get; set; }

    /// <summary>
    /// 业务错误码：用于细分业务场景的错误类型
    /// 业务意图：在发生业务错误时提供更精确的错误标识，便于调用方做特殊处理
    /// </summary>
    public int? ErrorCode { get; set; }

    /// <summary>
    /// 消息：对处理结果的文字描述
    /// 业务意图：提供人类可读的响应说明，便于调试和用户提示
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 数据：响应的业务数据
    /// 业务意图：承载实际的业务数据，可为单个对象或集合
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// 错误详情：包含额外的错误上下文信息
    /// 业务意图：在发生错误时提供详细的上下文数据，帮助调用方理解和处理错误
    /// </summary>
    public IDictionary<string, object>? ErrorDetails { get; set; }

    /// <summary>
    /// 创建成功响应
    /// 业务意图：快速生成成功的 API 响应，默认状态码 200
    /// </summary>
    /// <param name="data">返回的业务数据</param>
    /// <param name="message">成功消息</param>
    /// <returns>成功的 ApiResponse 对象</returns>
    public static ApiResponse Success(object? data = null, string message = "操作成功")
    {
        return new ApiResponse
        {
            Code = 200,
            Message = message,
            Data = data
        };
    }

    /// <summary>
    /// 创建失败响应
    /// 业务意图：快速生成失败的 API 响应，可自定义错误码和消息
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="code">HTTP 状态码，默认 400</param>
    /// <param name="errorCode">业务错误码</param>
    /// <param name="errorDetails">错误详情</param>
    /// <returns>失败的 ApiResponse 对象</returns>
    public static ApiResponse Fail(
        string message = "操作失败",
        int code = 400,
        int? errorCode = null,
        IDictionary<string, object>? errorDetails = null)
    {
        return new ApiResponse
        {
            Code = code,
            ErrorCode = errorCode,
            Message = message,
            Data = null,
            ErrorDetails = errorDetails
        };
    }
}
