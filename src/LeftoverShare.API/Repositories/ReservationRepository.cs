using LeftoverShare.API.Data;
using LeftoverShare.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeftoverShare.API.Repositories;

// 预订仓储实现
public class ReservationRepository : Repository<Reservation>, IReservationRepository
{
    public ReservationRepository(AppDbContext context) : base(context)
    {
    }

    // 分页获取预订详情（含帖子、发布者、领取者信息）
    public async Task<(IEnumerable<Reservation> Items, int TotalCount)> GetPagedWithDetailsAsync(int pageNumber, int pageSize)
    {
        var query = _dbSet
            .Include(r => r.Post).ThenInclude(p => p.Poster)
            .Include(r => r.Claimer);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.ReservedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    // 根据帖子ID获取预订列表（使用 PostId 而非 SharePostId）
    public async Task<IEnumerable<Reservation>> GetByPostIdAsync(int postId)
    {
        return await _dbSet
            .Include(r => r.Post).ThenInclude(p => p.Poster)
            .Include(r => r.Claimer)
            .Where(r => r.PostId == postId)
            .OrderByDescending(r => r.ReservedAt)
            .ToListAsync();
    }

    // 根据领取者ID获取预订列表（使用 ClaimerId）
    public async Task<IEnumerable<Reservation>> GetByClaimerIdAsync(int claimerId)
    {
        return await _dbSet
            .Include(r => r.Post).ThenInclude(p => p.Poster)
            .Include(r => r.Claimer)
            .Where(r => r.ClaimerId == claimerId)
            .OrderByDescending(r => r.ReservedAt)
            .ToListAsync();
    }

    // 根据用户ID获取预订列表（作为领取者）
    public async Task<IEnumerable<Reservation>> GetByUserIdAsync(int userId)
    {
        return await _dbSet
            .Include(r => r.Post).ThenInclude(p => p.Poster)
            .Include(r => r.Claimer)
            .Where(r => r.ClaimerId == userId)
            .OrderByDescending(r => r.ReservedAt)
            .ToListAsync();
    }

    // 根据ID获取预订（含帖子、发布者、领取者信息）
    public override async Task<Reservation?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(r => r.Post).ThenInclude(p => p.Poster)
            .Include(r => r.Claimer)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    // 获取所有预订（含帖子、发布者、领取者信息）
    public override async Task<IEnumerable<Reservation>> GetAllAsync()
    {
        return await _dbSet
            .Include(r => r.Post).ThenInclude(p => p.Poster)
            .Include(r => r.Claimer)
            .OrderByDescending(r => r.ReservedAt)
            .ToListAsync();
    }

    // 获取已删除的预订记录（按用户ID，分页）
    public async Task<(IEnumerable<Reservation> Items, int TotalCount)> GetDeletedPagedAsync(int userId, int pageNumber, int pageSize)
    {
        var query = _dbSet
            .IgnoreQueryFilters()
            .Include(r => r.Post).ThenInclude(p => p.Poster)
            .Include(r => r.Claimer)
            .Where(r => r.IsDeleted && (r.ClaimerId == userId || r.Post.PosterId == userId));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.DeletedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    // 根据ID获取预订（忽略软删除过滤器，含帖子、发布者、领取者信息）
    public override async Task<Reservation?> GetByIdIgnoreFilterAsync(int id)
    {
        return await _dbSet
            .IgnoreQueryFilters()
            .Include(r => r.Post).ThenInclude(p => p.Poster)
            .Include(r => r.Claimer)
            .FirstOrDefaultAsync(r => r.Id == id);
    }
}
