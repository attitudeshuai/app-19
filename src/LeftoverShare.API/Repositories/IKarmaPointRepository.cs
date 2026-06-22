using LeftoverShare.API.Entities;

namespace LeftoverShare.API.Repositories;

public interface IKarmaPointRepository : IRepository<KarmaPoint>
{
    new Task<KarmaPoint?> GetByIdIgnoreFilterAsync(int id);
    Task<(IEnumerable<KarmaPoint> Items, int TotalCount)> GetDeletedPagedAsync(int userId, int pageNumber, int pageSize);
    Task<IEnumerable<KarmaPoint>> GetByUserIdAsync(int userId);
    Task<int> GetTotalPointsByUserIdAsync(int userId);
}
