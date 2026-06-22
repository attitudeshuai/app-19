using LeftoverShare.API.Data;
using LeftoverShare.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeftoverShare.API.Repositories;

/// <summary>
/// 帖子标签仓储实现
/// </summary>
public class PostTagRepository : Repository<PostTag>, IPostTagRepository
{
    public PostTagRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<PostTag?> GetByCodeAsync(string code)
    {
        return await _dbSet.FirstOrDefaultAsync(pt => pt.Code == code);
    }

    public async Task<List<PostTag>> GetAllActiveAsync(bool includeUserDefined = true)
    {
        var query = _dbSet.Where(pt => pt.IsActive);
        if (!includeUserDefined)
        {
            query = query.Where(pt => pt.IsSystemDefined);
        }
        return await query
            .OrderBy(pt => pt.SortOrder)
            .ThenByDescending(pt => pt.UsageCount)
            .ThenBy(pt => pt.Name)
            .ToListAsync();
    }

    public async Task<List<PostTag>> GetPopularAsync(int topN = 20)
    {
        return await _dbSet
            .Where(pt => pt.IsActive)
            .OrderByDescending(pt => pt.UsageCount)
            .ThenBy(pt => pt.Name)
            .Take(topN)
            .ToListAsync();
    }

    public async Task<List<PostTag>> GetByUserIdAsync(int userId)
    {
        return await _dbSet
            .Where(pt => pt.CreatedBy == userId)
            .OrderByDescending(pt => pt.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<PostTag>> GetByIdsAsync(IEnumerable<int> ids)
    {
        return await _dbSet.Where(pt => ids.Contains(pt.Id)).ToListAsync();
    }

    public async Task<List<PostTag>> SearchByNameAsync(string keyword, int limit = 20)
    {
        return await _dbSet
            .Where(pt => pt.IsActive && pt.Name.Contains(keyword))
            .OrderByDescending(pt => pt.UsageCount)
            .ThenBy(pt => pt.Name)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<bool> IsUsedByPostsAsync(int tagId)
    {
        return await _context.SharePostPostTags.AnyAsync(sppt => sppt.PostTagId == tagId);
    }
}
