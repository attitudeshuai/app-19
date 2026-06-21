using LeftoverShare.API.Entities;

namespace LeftoverShare.API.Repositories;

public interface ISharePostRepository : IRepository<SharePost>
{
    Task<(IEnumerable<SharePost> Items, int TotalCount)> GetPagedWithDetailsAsync(int pageNumber, int pageSize);
    Task<IEnumerable<SharePost>> GetByUserIdAsync(int userId);
}
