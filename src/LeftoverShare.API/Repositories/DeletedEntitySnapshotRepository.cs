using LeftoverShare.API.Data;
using LeftoverShare.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeftoverShare.API.Repositories;

public class DeletedEntitySnapshotRepository : Repository<DeletedEntitySnapshot>, IDeletedEntitySnapshotRepository
{
    public DeletedEntitySnapshotRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<DeletedEntitySnapshot?> GetByEntityAsync(string entityType, int entityId)
    {
        return await _dbSet
            .Include(d => d.DeletedByUser)
            .FirstOrDefaultAsync(d => d.EntityType == entityType && d.EntityId == entityId);
    }

    public async Task<(IEnumerable<DeletedEntitySnapshot> Items, int TotalCount)> GetPagedByUserAsync(int? userId, string? entityType, int pageNumber, int pageSize)
    {
        var query = _dbSet.Include(d => d.DeletedByUser).AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(d => d.DeletedBy == userId.Value || d.OriginalOwnerId == userId.Value);
        }

        if (!string.IsNullOrEmpty(entityType))
        {
            query = query.Where(d => d.EntityType == entityType);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(d => d.DeletedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public override async Task<IEnumerable<DeletedEntitySnapshot>> GetAllAsync()
    {
        return await _dbSet
            .Include(d => d.DeletedByUser)
            .OrderByDescending(d => d.DeletedAt)
            .ToListAsync();
    }
}
