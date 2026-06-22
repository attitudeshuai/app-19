using LeftoverShare.API.Data;
using LeftoverShare.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeftoverShare.API.Repositories;

public class PublisherReputationRepository : Repository<PublisherReputation>, IPublisherReputationRepository
{
    public PublisherReputationRepository(AppDbContext context) : base(context)
    {
    }

    public override async Task<PublisherReputation?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(pr => pr.Publisher)
            .FirstOrDefaultAsync(pr => pr.Id == id);
    }

    public async Task<PublisherReputation?> GetByPublisherIdAsync(int publisherId)
    {
        return await _dbSet
            .Include(pr => pr.Publisher)
            .FirstOrDefaultAsync(pr => pr.PublisherId == publisherId);
    }

    public async Task<List<PublisherReputation>> GetLeaderboardAsync(int topN)
    {
        return await _dbSet
            .Include(pr => pr.Publisher)
            .OrderByDescending(pr => pr.ReputationScore)
            .ThenByDescending(pr => pr.TotalReviewCount)
            .Take(topN)
            .ToListAsync();
    }

    public async Task<List<PublisherReputation>> GetLeaderboardByRatingAsync(int topN)
    {
        return await _dbSet
            .Include(pr => pr.Publisher)
            .Where(pr => pr.TotalReviewCount >= 3)
            .OrderByDescending(pr => pr.AverageRating)
            .ThenByDescending(pr => pr.TotalReviewCount)
            .Take(topN)
            .ToListAsync();
    }
}
