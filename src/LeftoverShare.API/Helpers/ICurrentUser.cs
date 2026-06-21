namespace LeftoverShare.API.Helpers;

/// <summary>
/// 当前用户接口
/// 业务意图：定义获取当前登录用户 ID 的规范，便于在业务逻辑中获取用户身份信息
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// 当前登录用户 ID
    /// 业务意图：获取当前请求的用户标识，用于权限验证和数据关联
    /// </summary>
    int? UserId { get; }
}
