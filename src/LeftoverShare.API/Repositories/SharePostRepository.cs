using LeftoverShare.API.Data;
using LeftoverShare.API.Entities;
using LeftoverShare.API.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

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

    // 获取所有已过期但未标记为 Expired 状态的分享帖
    public async Task<IEnumerable<SharePost>> GetExpiredPostsAsync(DateTime now)
    {
        return await _dbSet
            .Include(sp => sp.Poster)
            .Include(sp => sp.Reservations)
                .ThenInclude(r => r.Claimer)
            .Where(sp => sp.AvailableUntil < now
                      && sp.Status != Entities.Enums.SharePostStatus.Expired
                      && sp.Status != Entities.Enums.SharePostStatus.Completed
                      && sp.Status != Entities.Enums.SharePostStatus.PickedUp)
            .ToListAsync();
    }

    // 获取已删除的帖子（含发布者信息，按用户ID，分页）
    public async Task<(IEnumerable<SharePost> Items, int TotalCount)> GetDeletedPagedWithDetailsAsync(int userId, int pageNumber, int pageSize)
    {
        var query = _dbSet
            .IgnoreQueryFilters()
            .Include(sp => sp.Poster)
            .Where(sp => sp.IsDeleted && sp.PosterId == userId);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(sp => sp.DeletedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    // 根据ID获取帖子（忽略软删除过滤器，含发布者信息）
    public override async Task<SharePost?> GetByIdIgnoreFilterAsync(int id)
    {
        return await _dbSet
            .IgnoreQueryFilters()
            .Include(sp => sp.Poster)
            .FirstOrDefaultAsync(sp => sp.Id == id);
    }

    /// <summary>
    /// 带行级锁获取分享帖（用于高并发库存扣减）
    /// 业务意图：使用 SELECT ... FOR UPDATE 锁定行，防止并发更新
    /// 注意：必须在事务中调用此方法，否则行级锁不会生效
    /// </summary>
    public async Task<SharePost?> GetByIdWithLockAsync(int id)
    {
        var currentTransaction = _context.Database.CurrentTransaction;
        if (currentTransaction == null)
        {
            throw new InvalidOperationException("GetByIdWithLockAsync 必须在数据库事务中调用");
        }

        return await _dbSet
            .FromSqlRaw(@"
                SELECT * 
                FROM SharePosts 
                WHERE Id = {0} 
                  AND IsDeleted = FALSE
                FOR UPDATE", id)
            .Include(sp => sp.Poster)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// 原子性扣减库存
    /// 业务意图：使用原生 SQL 执行原子性的库存扣减操作，
    /// 确保在高并发场景下不会出现超卖
    /// 关键点：
    /// 1. 使用 UPDATE 语句的原子性（数据库保证单条语句的原子性）
    /// 2. WHERE 条件中包含库存检查（ReservedQuantity + quantity <= Quantity）
    /// 3. 返回受影响的行数，通过返回值判断是否扣减成功
    /// </summary>
    public async Task<bool> TryDecrementReservedQuantityAsync(int postId, int quantity)
    {
        var rowsAffected = await _context.Database.ExecuteSqlRawAsync(@"
            UPDATE SharePosts 
            SET ReservedQuantity = ReservedQuantity + {0}
            WHERE Id = {1} 
              AND IsDeleted = FALSE
              AND Status = {2}
              AND ReservedQuantity + {0} <= Quantity
              AND ReservedQuantity >= 0",
            quantity,
            postId,
            SharePostStatus.Available.ToString());

        return rowsAffected > 0;
    }

    /// <summary>
    /// 原子性恢复库存
    /// 业务意图：使用原生 SQL 执行原子性的库存恢复操作，
    /// 用于取消预约或预约超时时恢复可用数量
    /// </summary>
    public async Task<bool> TryIncrementReservedQuantityAsync(int postId, int quantity)
    {
        var rowsAffected = await _context.Database.ExecuteSqlRawAsync(@"
            UPDATE SharePosts 
            SET ReservedQuantity = GREATEST(ReservedQuantity - {0}, 0)
            WHERE Id = {1} 
              AND IsDeleted = FALSE
              AND ReservedQuantity >= {0}",
            quantity,
            postId);

        return rowsAffected > 0;
    }

    /// <summary>
    /// 检查用户是否已预约该分享帖
    /// 业务意图：防止同一用户重复预约同一分享帖
    /// 注意：只检查未删除且未取消的预约
    /// </summary>
    public async Task<bool> HasExistingReservationAsync(int postId, int userId)
    {
        return await _context.Reservations
            .AnyAsync(r => r.PostId == postId
                        && r.ClaimerId == userId
                        && !r.IsDeleted
                        && r.Status != ReservationStatus.Cancelled);
    }

    /// <summary>
    /// 获取分享帖的可用库存
    /// 业务意图：计算剩余可用数量（总数量 - 已预约数量）
    /// </summary>
    public async Task<int> GetAvailableQuantityAsync(int postId)
    {
        var post = await _dbSet
            .Where(sp => sp.Id == postId && !sp.IsDeleted)
            .Select(sp => new { sp.Quantity, sp.ReservedQuantity })
            .FirstOrDefaultAsync();

        if (post == null)
        {
            return 0;
        }

        return Math.Max(0, post.Quantity - post.ReservedQuantity);
    }
}
