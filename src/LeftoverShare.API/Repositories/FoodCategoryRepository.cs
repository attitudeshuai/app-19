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

    public async Task<List<int>> GetAllDescendantIdsAsync(int categoryId)
    {
        var allCategories = await _dbSet
            .Where(fc => !fc.IsDeleted)
            .Select(fc => new { fc.Id, fc.ParentId })
            .ToListAsync();

        var lookup = allCategories
            .Where(c => c.ParentId.HasValue)
            .GroupBy(c => c.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(c => c.Id).ToList());

        var descendantIds = new List<int>();
        var queue = new Queue<int>();

        if (lookup.TryGetValue(categoryId, out var directChildren))
        {
            foreach (var childId in directChildren)
            {
                queue.Enqueue(childId);
            }
        }

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            descendantIds.Add(currentId);

            if (lookup.TryGetValue(currentId, out var children))
            {
                foreach (var childId in children)
                {
                    queue.Enqueue(childId);
                }
            }
        }

        return descendantIds;
    }

    public async Task<List<int>> GetAllAncestorIdsAsync(int categoryId)
    {
        var allCategories = await _dbSet
            .Where(fc => !fc.IsDeleted)
            .Select(fc => new { fc.Id, fc.ParentId })
            .ToDictionaryAsync(c => c.Id, c => c.ParentId);

        var ancestorIds = new List<int>();
        var visited = new HashSet<int>();
        int? currentId = categoryId;

        while (currentId.HasValue && allCategories.TryGetValue(currentId.Value, out var parentId))
        {
            if (!parentId.HasValue)
                break;

            if (visited.Contains(parentId.Value))
            {
                throw new InvalidOperationException($"检测到分类树存在循环引用，分类ID: {parentId.Value}");
            }

            visited.Add(parentId.Value);
            ancestorIds.Add(parentId.Value);
            currentId = parentId.Value;
        }

        return ancestorIds;
    }
}
