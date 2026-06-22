using LeftoverShare.API.Data;
using LeftoverShare.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeftoverShare.API.Repositories;

// 积分仓储实现
public class KarmaPointRepository : Repository<KarmaPoint>, IKarmaPointRepository
{
    public KarmaPointRepository(AppDbContext context) : base(context)
    {
    }

    // 根据用户ID获取积分记录（含用户信息）
    public async Task<IEnumerable<KarmaPoint>> GetByUserIdAsync(int userId)
    {
        return await _dbSet
            .Include(kp => kp.User)
            .Where(kp => kp.UserId == userId)
            .OrderByDescending(kp => kp.CreatedAt)
            .ToListAsync();
    }

    // 根据用户ID获取总积分
    public async Task<int> GetTotalPointsByUserIdAsync(int userId)
    {
        return await _dbSet
            .Where(kp => kp.UserId == userId)
            .SumAsync(kp => kp.Points);
    }

    // 根据ID获取积分记录（含用户信息）
    public override async Task<KarmaPoint?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(kp => kp.User)
            .FirstOrDefaultAsync(kp => kp.Id == id);
    }

    // 获取所有积分记录（含用户信息）
    public override async Task<IEnumerable<KarmaPoint>> GetAllAsync()
    {
        return await _dbSet
            .Include(kp => kp.User)
            .OrderByDescending(kp => kp.CreatedAt)
            .ToListAsync();
    }

    // 根据ID获取积分记录（忽略软删除过滤器，含用户信息）
    public override async Task<KarmaPoint?> GetByIdIgnoreFilterAsync(int id)
    {
        return await _dbSet
            .IgnoreQueryFilters()
            .Include(kp => kp.User)
            .FirstOrDefaultAsync(kp => kp.Id == id);
    }

    // 获取已删除的积分记录（按用户ID，分页）
    public async Task<(IEnumerable<KarmaPoint> Items, int TotalCount)> GetDeletedPagedAsync(int userId, int pageNumber, int pageSize)
    {
        var query = _dbSet
            .IgnoreQueryFilters()
            .Include(kp => kp.User)
            .Where(kp => kp.IsDeleted && kp.UserId == userId);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(kp => kp.DeletedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
