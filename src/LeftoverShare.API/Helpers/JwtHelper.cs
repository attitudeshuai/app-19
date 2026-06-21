using LeftoverShare.API.Entities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LeftoverShare.API.Helpers;

/// <summary>
/// JWT 令牌助手类
/// 业务意图：提供生成 JWT 令牌的功能，用于用户身份认证
/// </summary>
public static class JwtHelper
{
    /// <summary>
    /// 生成 JWT 令牌
    /// 业务意图：根据用户信息和 JWT 配置生成包含用户声明的 Bearer 令牌
    /// </summary>
    /// <param name="user">用户实体，包含用户的基本信息</param>
    /// <param name="settings">JWT 配置，包含密钥、颁发者、受众和过期时间</param>
    /// <returns>生成的 JWT 令牌字符串</returns>
    public static string GenerateToken(User user, JwtSettings settings)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            expires: DateTime.Now.AddMinutes(settings.ExpiryInMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
