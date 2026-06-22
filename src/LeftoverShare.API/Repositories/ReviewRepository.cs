using LeftoverShare.API.Data;
using LeftoverShare.API.Entities;
using LeftoverShare.API.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace LeftoverShare.API.Repositories;

public class ReviewRepository : Repository<Review>, IReviewRepository
{
    public ReviewRepository(AppDbContext context) : base(context)
    {
    }

    public override async Task<Review?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(r => r.Reservation)
            .Include(r => r.Reviewer)
            .Include(r => r.Publisher)
            .Include(r => r.SharePost)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Review?> GetByReservationAndReviewerAsync(int reservationId, int reviewerId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(r => r.ReservationId == reservationId && r.ReviewerId == reviewerId);
    }

    public async Task<IEnumerable<Review>> GetByPublisherIdAsync(int publisherId, int? rating = null)
    {
        var query = _dbSet
            .Include(r => r.Reviewer)
            .Include(r => r.SharePost)
            .Where(r => r.PublisherId == publisherId && r.Status == ReviewStatus.Normal);

        if (rating.HasValue)
        {
            query = query.Where(r => r.Rating == rating.Value);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Review>> GetBySharePostIdAsync(int sharePostId)
    {
        return await _dbSet
            .Include(r => r.Reviewer)
            .Include(r => r.Publisher)
            .Where(r => r.SharePostId == sharePostId && r.Status == ReviewStatus.Normal)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Review>> GetByReviewerIdAsync(int reviewerId)
    {
        return await _dbSet
            .Include(r => r.Publisher)
            .Include(r => r.SharePost)
            .Where(r => r.ReviewerId == reviewerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> CountByPublisherIdAsync(int publisherId)
    {
        return await _dbSet
            .CountAsync(r => r.PublisherId == publisherId && r.Status == ReviewStatus.Normal);
    }

    public async Task<int> CountByReviewerIdAsync(int reviewerId)
    {
        return await _dbSet.CountAsync(r => r.ReviewerId == reviewerId);
    }

    public async Task<int> CountByReviewerIpAsync(string ip, TimeSpan within)
    {
        var cutoff = DateTime.UtcNow - within;
        return await _dbSet.CountAsync(r => r.ReviewerIp == ip && r.CreatedAt >= cutoff);
    }

    public async Task<decimal> GetAverageRatingByPublisherIdAsync(int publisherId)
    {
        var reviews = await _dbSet
            .Where(r => r.PublisherId == publisherId && r.Status == ReviewStatus.Normal)
            .ToListAsync();

        if (!reviews.Any()) return 0m;
        return Math.Round((decimal)reviews.Average(r => r.Rating), 2);
    }

    public async Task<Dictionary<int, int>> GetRatingDistributionByPublisherIdAsync(int publisherId)
    {
        var distribution = await _dbSet
            .Where(r => r.PublisherId == publisherId && r.Status == ReviewStatus.Normal)
            .GroupBy(r => r.Rating)
            .Select(g => new { Rating = g.Key, Count = g.Count() })
            .ToListAsync();

        return distribution.ToDictionary(d => d.Rating, d => d.Count);
    }

    public async Task<(IEnumerable<Review> Items, int TotalCount)> GetPagedByPublisherIdAsync(
        int publisherId, int pageNumber, int pageSize)
    {
        var query = _dbSet
            .Include(r => r.Reviewer)
            .Include(r => r.SharePost)
            .Where(r => r.PublisherId == publisherId && r.Status == ReviewStatus.Normal);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(IEnumerable<Review> Items, int TotalCount)> GetPagedBySharePostIdAsync(
        int sharePostId, int pageNumber, int pageSize)
    {
        var query = _dbSet
            .Include(r => r.Reviewer)
            .Where(r => r.SharePostId == sharePostId && r.Status == ReviewStatus.Normal);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<List<(int PublisherId, decimal AverageRating, int ReviewCount)>> GetTopRatedPublishersAsync(int topN)
    {
        var result = await _dbSet
            .Where(r => r.Status == ReviewStatus.Normal)
            .GroupBy(r => r.PublisherId)
            .Select(g => new
            {
                PublisherId = g.Key,
                AverageRating = Math.Round((decimal)g.Average(r => r.Rating), 2),
                ReviewCount = g.Count()
            })
            .OrderByDescending(x => x.AverageRating)
            .ThenByDescending(x => x.ReviewCount)
            .Take(topN)
            .ToListAsync();

        return result.Select(x => (x.PublisherId, x.AverageRating, x.ReviewCount)).ToList();
    }
}
