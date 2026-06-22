using LeftoverShare.API.Data;
using LeftoverShare.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeftoverShare.API.Repositories;

/// <summary>
/// 过敏原标签仓储实现
/// </summary>
public class AllergenTagRepository : Repository<AllergenTag>, IAllergenTagRepository
{
    public AllergenTagRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<AllergenTag?> GetByCodeAsync(string code)
    {
        return await _dbSet.FirstOrDefaultAsync(at => at.Code == code);
    }

    public async Task<List<AllergenTag>> GetAllActiveAsync()
    {
        return await _dbSet
            .Where(at => at.IsActive)
            .OrderBy(at => at.SortOrder)
            .ThenByDescending(at => at.SeverityLevel)
            .ThenBy(at => at.Name)
            .ToListAsync();
    }

    public async Task<List<AllergenTag>> GetBySeverityAsync(int severityLevel, bool includeInactive = false)
    {
        var query = _dbSet.Where(at => at.SeverityLevel == severityLevel);
        if (!includeInactive)
        {
            query = query.Where(at => at.IsActive);
        }
        return await query.OrderBy(at => at.SortOrder).ThenBy(at => at.Name).ToListAsync();
    }

    public async Task<List<AllergenTag>> GetByIdsAsync(IEnumerable<int> ids)
    {
        return await _dbSet.Where(at => ids.Contains(at.Id)).ToListAsync();
    }

    public async Task<bool> IsUsedByPostsAsync(int tagId)
    {
        return await _context.SharePostAllergenTags.AnyAsync(spat => spat.AllergenTagId == tagId);
    }
}
