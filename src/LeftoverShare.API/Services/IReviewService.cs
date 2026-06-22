using LeftoverShare.API.DTOs.Common;
using LeftoverShare.API.DTOs.Reviews;
using LeftoverShare.API.Helpers;

namespace LeftoverShare.API.Services;

public interface IReviewService
{
    Task<ApiResponse> CreateReviewAsync(int reviewerId, CreateReviewRequest request, string? reviewerIp = null);
    Task<ApiResponse> GetReviewsByPublisherAsync(int publisherId, PagedRequest request);
    Task<ApiResponse> GetReviewsBySharePostAsync(int sharePostId, PagedRequest request);
    Task<ApiResponse> GetMyReviewsAsync(int reviewerId, PagedRequest request);
    Task<ApiResponse> GetReviewByIdAsync(int id);
    Task<ApiResponse> GetPublisherReputationAsync(int publisherId);
    Task<ApiResponse> GetReviewAggregationAsync(int publisherId);
    Task<ApiResponse> GetLeaderboardAsync(int topN = 10);
    Task<ApiResponse> GetLeaderboardByRatingAsync(int topN = 10);
    Task<ApiResponse> DeleteReviewAsync(int id, int userId);
}
