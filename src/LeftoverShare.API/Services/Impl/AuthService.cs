using AutoMapper;
using BCrypt.Net;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LeftoverShare.API.DTOs.Auth;
using LeftoverShare.API.Entities.Enums;
using LeftoverShare.API.Helpers;
using LeftoverShare.API.Repositories;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using UserEntity = LeftoverShare.API.Entities.User;

namespace LeftoverShare.API.Services.Impl;

/// <summary>
/// 认证服务实现
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly JwtSettings _jwtSettings;

    public AuthService(IUnitOfWork unitOfWork, IMapper mapper, IOptions<JwtSettings> jwtSettings)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _jwtSettings = jwtSettings.Value;
    }

    /// <summary>
    /// 用户注册：检查用户名/邮箱唯一性，BCrypt哈希密码，生成JWT令牌
    /// </summary>
    public async Task<ApiResponse> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _unitOfWork.Users.GetByUsernameAsync(request.Username);
        if (existingUser != null)
        {
            return ApiResponse.Fail("用户名已存在");
        }

        existingUser = await _unitOfWork.Users.GetByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return ApiResponse.Fail("邮箱已被注册");
        }

        var user = new UserEntity
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsActive = true,
            TotalKarmaPoints = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var token = GenerateJwtToken(user);
        var userResponse = _mapper.Map<UserResponse>(user);

        return ApiResponse.Success(new { User = userResponse, Token = token }, "注册成功");
    }

    /// <summary>
    /// 用户登录：验证凭据，检查IsActive状态，生成JWT令牌
    /// </summary>
    public async Task<ApiResponse> LoginAsync(LoginRequest request)
    {
        var user = await _unitOfWork.Users.GetByUsernameOrEmailAsync(request.UsernameOrEmail);
        if (user == null)
        {
            return ApiResponse.Fail("用户名或密码错误");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return ApiResponse.Fail("用户名或密码错误");
        }

        if (!user.IsActive)
        {
            return ApiResponse.Fail("账户已被禁用", 403);
        }

        var token = GenerateJwtToken(user);
        var userResponse = _mapper.Map<UserResponse>(user);

        return ApiResponse.Success(new { User = userResponse, Token = token }, "登录成功");
    }

    /// <summary>
    /// 获取当前用户信息：获取用户+总积分
    /// </summary>
    public async Task<ApiResponse> GetCurrentUserAsync(int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            return ApiResponse.Fail("用户不存在", 404);
        }

        var totalPoints = await _unitOfWork.KarmaPoints.GetTotalPointsByUserIdAsync(userId);
        user.TotalKarmaPoints = totalPoints;

        var userResponse = _mapper.Map<UserResponse>(user);
        return ApiResponse.Success(userResponse);
    }

    /// <summary>
    /// 更新用户资料：更新用户名/邮箱/头像，检查唯一性
    /// </summary>
    public async Task<ApiResponse> UpdateProfileAsync(int userId, UpdateProfileRequest request)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            return ApiResponse.Fail("用户不存在", 404);
        }

        if (user.Username != request.Username)
        {
            var existingUser = await _unitOfWork.Users.GetByUsernameAsync(request.Username);
            if (existingUser != null && existingUser.Id != userId)
            {
                return ApiResponse.Fail("用户名已被使用");
            }
            user.Username = request.Username;
        }

        if (user.Email != request.Email)
        {
            var existingUser = await _unitOfWork.Users.GetByEmailAsync(request.Email);
            if (existingUser != null && existingUser.Id != userId)
            {
                return ApiResponse.Fail("邮箱已被使用");
            }
            user.Email = request.Email;
        }

        user.Avatar = request.Avatar;
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        var userResponse = _mapper.Map<UserResponse>(user);
        return ApiResponse.Success(userResponse, "资料更新成功");
    }

    /// <summary>
    /// 生成JWT令牌
    /// </summary>
    private string GenerateJwtToken(UserEntity user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryInMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
