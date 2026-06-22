using LeftoverShare.API.Entities;

namespace LeftoverShare.API.Repositories;

public interface IPublisherReputationRepository : IRepository<PublisherReputation>
{
    Task<PublisherReputation?> GetByPublisherIdAsync(int publisherId);
    Task<List<PublisherReputation>> GetLeaderboardAsync(int topN);
    Task<List<PublisherReputation>> GetLeaderboardByRatingAsync(int topN);
}
