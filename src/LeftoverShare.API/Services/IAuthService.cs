using LeftoverShare.API.DTOs.Auth;
using LeftoverShare.API.Helpers;

namespace LeftoverShare.API.Services;

/// <summary>
/// 认证服务接口
/// 业务意图：定义用户认证相关的操作，包括注册、登录和资料更新
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// 用户注册
    /// 业务意图：创建新用户账户，验证用户名和邮箱唯一性，使用BCrypt哈希密码后存储
    /// </summary>
    /// <param name="request">注册请求参数</param>
    /// <returns>包含用户信息和JWT令牌的响应</returns>
    Task<ApiResponse> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// 用户登录
    /// 业务意图：验证用户凭据，使用BCrypt验证密码哈希，验证通过后生成JWT令牌
    /// </summary>
    /// <param name="request">登录请求参数</param>
    /// <returns>包含用户信息和JWT令牌的响应</returns>
    Task<ApiResponse> LoginAsync(LoginRequest request);

    /// <summary>
    /// 获取当前用户信息
    /// 业务意图：根据用户ID获取用户详细信息，计算用户总积分
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>包含用户详细信息的响应</returns>
    Task<ApiResponse> GetCurrentUserAsync(int userId);

    /// <summary>
    /// 更新用户个人资料
    /// 业务意图：允许用户更新自己的用户名、邮箱和头像，验证用户名和邮箱的唯一性
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="request">更新资料请求参数</param>
    /// <returns>包含更新后用户信息的响应</returns>
    Task<ApiResponse> UpdateProfileAsync(int userId, UpdateProfileRequest request);
}
