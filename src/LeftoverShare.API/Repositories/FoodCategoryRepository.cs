using LeftoverShare.API.Data;
using LeftoverShare.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeftoverShare.API.Repositories;

/// <summary>
/// 食物分类仓储实现
/// </summary>
public class FoodCategoryRepository : Repository<FoodCategory>, IFoodCategoryRepository
{
    public FoodCategoryRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<FoodCategory?> GetByCodeAsync(string code)
    {
        return await _dbSet.FirstOrDefaultAsync(fc => fc.Code == code);
    }

    public async Task<List<FoodCategory>> GetRootCategoriesAsync(bool includeInactive = false)
    {
        var query = _dbSet.Where(fc => fc.ParentId == null);
        if (!includeInactive)
        {
            query = query.Where(fc => fc.IsActive);
        }
        return await query.OrderBy(fc => fc.SortOrder).ThenBy(fc => fc.Name).ToListAsync();
    }

    public async Task<List<FoodCategory>> GetChildrenAsync(int parentId, bool includeInactive = false)
    {
        var query = _dbSet.Where(fc => fc.ParentId == parentId);
        if (!includeInactive)
        {
            query = query.Where(fc => fc.IsActive);
        }
        return await query.OrderBy(fc => fc.SortOrder).ThenBy(fc => fc.Name).ToListAsync();
    }

    public async Task<List<FoodCategory>> GetAllActiveWithHierarchyAsync()
    {
        return await _dbSet
            .Where(fc => fc.IsActive)
            .Include(fc => fc.Children)
            .OrderBy(fc => fc.ParentId.HasValue ? 1 : 0)
            .ThenBy(fc => fc.SortOrder)
            .ThenBy(fc => fc.Name)
            .ToListAsync();
    }

    public async Task<bool> HasChildrenAsync(int categoryId)
    {
        return await _dbSet.AnyAsync(fc => fc.ParentId == categoryId);
    }

    public async Task<bool> IsUsedByPostsAsync(int categoryId)
    {
        return await _context.SharePosts.AnyAsync(sp => sp.FoodCategoryId == categoryId);
    }
}
