using LeftoverShare.API.Data;
using LeftoverShare.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeftoverShare.API.Repositories;

// 分享帖子仓储实现
public class SharePostRepository : Repository<SharePost>, ISharePostRepository
{
    public SharePostRepository(AppDbContext context) : base(context)
    {
    }

    // 分页获取帖子详情（含发布者信息）
    public async Task<(IEnumerable<SharePost> Items, int TotalCount)> GetPagedWithDetailsAsync(int pageNumber, int pageSize)
    {
        var query = _dbSet
            .Include(sp => sp.Poster);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(sp => sp.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    // 根据用户ID获取帖子列表（使用 PosterId）
    public async Task<IEnumerable<SharePost>> GetByUserIdAsync(int userId)
    {
        return await _dbSet
            .Include(sp => sp.Poster)
            .Where(sp => sp.PosterId == userId)
            .OrderByDescending(sp => sp.CreatedAt)
            .ToListAsync();
    }

    // 根据ID获取帖子（含发布者信息）
    public override async Task<SharePost?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(sp => sp.Poster)
            .FirstOrDefaultAsync(sp => sp.Id == id);
    }

    // 获取所有帖子（含发布者信息）
    public override async Task<IEnumerable<SharePost>> GetAllAsync()
    {
        return await _dbSet
            .Include(sp => sp.Poster)
            .OrderByDescending(sp => sp.CreatedAt)
            .ToListAsync();
    }
}
