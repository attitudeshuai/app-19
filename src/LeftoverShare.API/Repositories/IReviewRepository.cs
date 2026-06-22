using LeftoverShare.API.Entities;

namespace LeftoverShare.API.Repositories;

public interface IReviewRepository : IRepository<Review>
{
    Task<Review?> GetByReservationAndReviewerAsync(int reservationId, int reviewerId);
    Task<IEnumerable<Review>> GetByPublisherIdAsync(int publisherId, int? rating = null);
    Task<IEnumerable<Review>> GetBySharePostIdAsync(int sharePostId);
    Task<IEnumerable<Review>> GetByReviewerIdAsync(int reviewerId);
    Task<int> CountByPublisherIdAsync(int publisherId);
    Task<int> CountByReviewerIdAsync(int reviewerId);
    Task<int> CountByReviewerIpAsync(string ip, TimeSpan within);
    Task<decimal> GetAverageRatingByPublisherIdAsync(int publisherId);
    Task<Dictionary<int, int>> GetRatingDistributionByPublisherIdAsync(int publisherId);
    Task<(IEnumerable<Review> Items, int TotalCount)> GetPagedByPublisherIdAsync(int publisherId, int pageNumber, int pageSize);
    Task<(IEnumerable<Review> Items, int TotalCount)> GetPagedBySharePostIdAsync(int sharePostId, int pageNumber, int pageSize);
    Task<List<(int PublisherId, decimal AverageRating, int ReviewCount)>> GetTopRatedPublishersAsync(int topN);
}
