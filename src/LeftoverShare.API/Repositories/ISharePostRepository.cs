using LeftoverShare.API.Entities;

namespace LeftoverShare.API.Repositories;

public interface ISharePostRepository : IRepository<SharePost>
{
    Task<(IEnumerable<SharePost> Items, int TotalCount)> GetPagedWithDetailsAsync(int pageNumber, int pageSize);
    Task<(IEnumerable<SharePost> Items, int TotalCount)> GetDeletedPagedWithDetailsAsync(int userId, int pageNumber, int pageSize);
    new Task<SharePost?> GetByIdIgnoreFilterAsync(int id);
    Task<IEnumerable<SharePost>> GetByUserIdAsync(int userId);
    Task<IEnumerable<SharePost>> GetExpiredPostsAsync(DateTime now);
}
