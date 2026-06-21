namespace LeftoverShare.API.Helpers;

/// <summary>
/// JWT 配置类
/// 业务意图：存储 JWT 令牌生成和验证所需的配置参数
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// 密钥：用于签名和验证 JWT 令牌的安全密钥
    /// 业务意图：确保令牌的完整性和真实性，防止伪造
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// 颁发者：生成 JWT 令牌的服务器标识
    /// 业务意图：验证令牌的来源，确保由可信的服务器颁发
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// 受众：JWT 令牌的预期接收方
    /// 业务意图：验证令牌的目标受众，确保令牌用于正确的应用
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// 过期时间（分钟）：JWT 令牌的有效时长
    /// 业务意图：限制令牌的有效期，降低安全风险
    /// </summary>
    public int ExpiryInMinutes { get; set; } = 120;
}
