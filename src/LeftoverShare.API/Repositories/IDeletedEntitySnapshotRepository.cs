using LeftoverShare.API.Entities;

namespace LeftoverShare.API.Repositories;

public interface IDeletedEntitySnapshotRepository : IRepository<DeletedEntitySnapshot>
{
    Task<DeletedEntitySnapshot?> GetByEntityAsync(string entityType, int entityId);
    Task<(IEnumerable<DeletedEntitySnapshot> Items, int TotalCount)> GetPagedByUserAsync(int? userId, string? entityType, int pageNumber, int pageSize);
}
