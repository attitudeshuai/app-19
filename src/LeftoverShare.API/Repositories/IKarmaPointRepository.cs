using LeftoverShare.API.Entities;

namespace LeftoverShare.API.Repositories;

public interface IKarmaPointRepository : IRepository<KarmaPoint>
{
    Task<IEnumerable<KarmaPoint>> GetByUserIdAsync(int userId);
    Task<int> GetTotalPointsByUserIdAsync(int userId);
}
