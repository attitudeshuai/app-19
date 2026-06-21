using LeftoverShare.API.Entities;

namespace LeftoverShare.API.Repositories;

// 用户仓储接口，继承泛型仓储
public interface IUserRepository : IRepository<User>
{
    // 根据用户名获取用户
    Task<User?> GetByUsernameAsync(string username);

    // 根据邮箱获取用户
    Task<User?> GetByEmailAsync(string email);

    // 根据用户名或邮箱获取用户（用于登录）
    Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail);
}
