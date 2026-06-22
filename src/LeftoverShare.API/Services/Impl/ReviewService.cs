using AutoMapper;
using LeftoverShare.API.DTOs.Common;
using LeftoverShare.API.DTOs.Reviews;
using LeftoverShare.API.Entities;
using LeftoverShare.API.Entities.Enums;
using LeftoverShare.API.Helpers;
using LeftoverShare.API.Repositories;

namespace LeftoverShare.API.Services.Impl;

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    private const int MaxReviewsPerDayPerIp = 10;
    private const int MaxLowRatingReviewsPerDayPerUser = 3;
    private const int MinReviewsForReputation = 1;
    private const decimal BaseReputationScore = 50m;
    private const decimal MaxReputationScore = 100m;
    private const decimal MinReputationScore = 0m;

    public ReviewService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse> CreateReviewAsync(int reviewerId, CreateReviewRequest request, string? reviewerIp = null)
    {
        var reservation = await _unitOfWork.Reservations.GetByIdAsync(request.ReservationId);
        if (reservation == null)
        {
            return ApiResponse.Fail("预约不存在", 404);
        }

        if (reservation.ClaimerId != reviewerId)
        {
            return ApiResponse.Fail("只能评价自己的预约", 403);
        }

        if (reservation.Status != ReservationStatus.Completed)
        {
            return ApiResponse.Fail("只能评价已完成的预约");
        }

        var existingReview = await _unitOfWork.Reviews.GetByReservationAndReviewerAsync(
            request.ReservationId, reviewerId);
        if (existingReview != null)
        {
            return ApiResponse.Fail("该预约已评价，不可重复评价");
        }

        var antiCheatResult = await RunAntiCheatChecksAsync(reviewerId, reviewerIp, request.Rating);
        var reviewStatus = antiCheatResult.IsClean ? ReviewStatus.Normal : ReviewStatus.Suspected;

        var post = await _unitOfWork.SharePosts.GetByIdAsync(reservation.PostId);
        if (post == null)
        {
            return ApiResponse.Fail("关联帖子不存在", 404);
        }

        var isFirstReview = !await HasReviewerReviewedPublisherAsync(reviewerId, post.PosterId);

        var review = new Review
        {
            ReservationId = request.ReservationId,
            ReviewerId = reviewerId,
            PublisherId = post.PosterId,
            SharePostId = post.Id,
            Rating = request.Rating,
            Comment = request.Comment,
            Status = reviewStatus,
            ReviewerIp = reviewerIp,
            IsFirstReview = isFirstReview,
            FlagReason = antiCheatResult.IsClean ? null : antiCheatResult.FlagCode,
            FlagDetail = antiCheatResult.IsClean ? null : antiCheatResult.FlagMessage,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Reviews.AddAsync(review);

        if (reviewStatus == ReviewStatus.Normal)
        {
            await UpdatePublisherReputationAsync(post.PosterId, request.Rating, isAdd: true);
        }

        await _unitOfWork.SaveChangesAsync();

        var reviewResponse = _mapper.Map<ReviewResponse>(review);
        var message = reviewStatus == ReviewStatus.Normal ? "评价成功" : "评价已提交，系统正在审核中";
        return ApiResponse.Success(reviewResponse, message);
    }

    public async Task<ApiResponse> GetReviewsByPublisherAsync(int publisherId, PagedRequest request)
    {
        var (items, totalCount) = await _unitOfWork.Reviews.GetPagedByPublisherIdAsync(
            publisherId, request.PageNumber, request.PageSize);

        var reviewResponses = _mapper.Map<List<ReviewResponse>>(items);
        var pagedResponse = new PagedResponse<ReviewResponse>
        {
            Items = reviewResponses,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
        };

        return ApiResponse.Success(pagedResponse);
    }

    public async Task<ApiResponse> GetReviewsBySharePostAsync(int sharePostId, PagedRequest request)
    {
        var (items, totalCount) = await _unitOfWork.Reviews.GetPagedBySharePostIdAsync(
            sharePostId, request.PageNumber, request.PageSize);

        var reviewResponses = _mapper.Map<List<ReviewResponse>>(items);
        var pagedResponse = new PagedResponse<ReviewResponse>
        {
            Items = reviewResponses,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
        };

        return ApiResponse.Success(pagedResponse);
    }

    public async Task<ApiResponse> GetMyReviewsAsync(int reviewerId, PagedRequest request)
    {
        var allReviews = await _unitOfWork.Reviews.GetByReviewerIdAsync(reviewerId);
        var totalCount = allReviews.Count();
        var pagedItems = allReviews
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var reviewResponses = _mapper.Map<List<ReviewResponse>>(pagedItems);
        var pagedResponse = new PagedResponse<ReviewResponse>
        {
            Items = reviewResponses,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
        };

        return ApiResponse.Success(pagedResponse);
    }

    public async Task<ApiResponse> GetReviewByIdAsync(int id)
    {
        var review = await _unitOfWork.Reviews.GetByIdAsync(id);
        if (review == null)
        {
            return ApiResponse.Fail("评价不存在", 404);
        }

        var reviewResponse = _mapper.Map<ReviewResponse>(review);
        return ApiResponse.Success(reviewResponse);
    }

    public async Task<ApiResponse> GetPublisherReputationAsync(int publisherId)
    {
        var reputation = await _unitOfWork.PublisherReputations.GetByPublisherIdAsync(publisherId);

        if (reputation == null)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(publisherId);
            if (user == null)
            {
                return ApiResponse.Fail("用户不存在", 404);
            }

            var defaultReputation = new PublisherReputationResponse
            {
                PublisherId = publisherId,
                PublisherName = user.Username,
                AverageRating = 0m,
                TotalReviewCount = 0,
                NormalReviewCount = 0,
                ReputationScore = BaseReputationScore
            };
            return ApiResponse.Success(defaultReputation);
        }

        var reputationResponse = _mapper.Map<PublisherReputationResponse>(reputation);
        return ApiResponse.Success(reputationResponse);
    }

    public async Task<ApiResponse> GetReviewAggregationAsync(int publisherId)
    {
        var distribution = await _unitOfWork.Reviews.GetRatingDistributionByPublisherIdAsync(publisherId);
        var avgRating = await _unitOfWork.Reviews.GetAverageRatingByPublisherIdAsync(publisherId);
        var totalCount = await _unitOfWork.Reviews.CountByPublisherIdAsync(publisherId);

        var user = await _unitOfWork.Users.GetByIdAsync(publisherId);
        if (user == null)
        {
            return ApiResponse.Fail("用户不存在", 404);
        }

        var aggregation = new ReviewAggregationResponse
        {
            PublisherId = publisherId,
            PublisherName = user.Username,
            AverageRating = avgRating,
            TotalReviewCount = totalCount,
            FiveStarCount = distribution.GetValueOrDefault(5, 0),
            FourStarCount = distribution.GetValueOrDefault(4, 0),
            ThreeStarCount = distribution.GetValueOrDefault(3, 0),
            TwoStarCount = distribution.GetValueOrDefault(2, 0),
            OneStarCount = distribution.GetValueOrDefault(1, 0)
        };

        return ApiResponse.Success(aggregation);
    }

    public async Task<ApiResponse> GetLeaderboardAsync(int topN = 10)
    {
        var reputations = await _unitOfWork.PublisherReputations.GetLeaderboardAsync(topN);
        var leaderboard = new List<ReviewLeaderboardEntry>();

        for (int i = 0; i < reputations.Count; i++)
        {
            var rep = reputations[i];
            leaderboard.Add(new ReviewLeaderboardEntry
            {
                PublisherId = rep.PublisherId,
                PublisherName = rep.Publisher?.Username ?? string.Empty,
                ReputationScore = rep.ReputationScore,
                AverageRating = rep.AverageRating,
                TotalReviewCount = rep.TotalReviewCount,
                Rank = i + 1
            });
        }

        return ApiResponse.Success(leaderboard);
    }

    public async Task<ApiResponse> GetLeaderboardByRatingAsync(int topN = 10)
    {
        var reputations = await _unitOfWork.PublisherReputations.GetLeaderboardByRatingAsync(topN);
        var leaderboard = new List<ReviewLeaderboardEntry>();

        for (int i = 0; i < reputations.Count; i++)
        {
            var rep = reputations[i];
            leaderboard.Add(new ReviewLeaderboardEntry
            {
                PublisherId = rep.PublisherId,
                PublisherName = rep.Publisher?.Username ?? string.Empty,
                ReputationScore = rep.ReputationScore,
                AverageRating = rep.AverageRating,
                TotalReviewCount = rep.TotalReviewCount,
                Rank = i + 1
            });
        }

        return ApiResponse.Success(leaderboard);
    }

    public async Task<ApiResponse> DeleteReviewAsync(int id, int userId)
    {
        var review = await _unitOfWork.Reviews.GetByIdAsync(id);
        if (review == null)
        {
            return ApiResponse.Fail("评价不存在", 404);
        }

        if (review.ReviewerId != userId)
        {
            return ApiResponse.Fail("无权删除此评价", 403);
        }

        if (review.Status == ReviewStatus.Normal)
        {
            await UpdatePublisherReputationAsync(review.PublisherId, review.Rating, isAdd: false);
        }

        review.DeletedBy = userId;
        review.DeletionReason = "用户删除评价";
        _unitOfWork.Reviews.Delete(review);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse.Success(null, "评价已删除");
    }

    private async Task<AntiCheatResult> RunAntiCheatChecksAsync(int reviewerId, string? reviewerIp, int rating)
    {
        if (!string.IsNullOrEmpty(reviewerIp))
        {
            var ipReviewCount = await _unitOfWork.Reviews.CountByReviewerIpAsync(
                reviewerIp, TimeSpan.FromHours(24));
            if (ipReviewCount >= MaxReviewsPerDayPerIp)
            {
                return AntiCheatResult.Flagged(1, $"同一IP 24小时内评价数超过{MaxReviewsPerDayPerIp}次");
            }
        }

        if (rating <= 2)
        {
            var recentLowRatings = await _unitOfWork.Reviews.GetByReviewerIdAsync(reviewerId);
            var recentCount = recentLowRatings.Count(r =>
                r.Rating <= 2 &&
                r.CreatedAt >= DateTime.UtcNow.AddHours(-24));

            if (recentCount >= MaxLowRatingReviewsPerDayPerUser)
            {
                return AntiCheatResult.Flagged(2,
                    $"24小时内低分评价数超过{MaxLowRatingReviewsPerDayPerUser}次");
            }
        }

        var userReviewCount = await _unitOfWork.Reviews.CountByReviewerIdAsync(reviewerId);
        var recentReviews = (await _unitOfWork.Reviews.GetByReviewerIdAsync(reviewerId))
            .Where(r => r.CreatedAt >= DateTime.UtcNow.AddHours(1))
            .ToList();

        if (recentReviews.Count >= 5)
        {
            return AntiCheatResult.Flagged(3, "1小时内评价数超过5次，疑似刷分行为");
        }

        return AntiCheatResult.Clean();
    }

    private async Task<bool> HasReviewerReviewedPublisherAsync(int reviewerId, int publisherId)
    {
        var reviews = await _unitOfWork.Reviews.GetByPublisherIdAsync(publisherId);
        return reviews.Any(r => r.ReviewerId == reviewerId);
    }

    private async Task UpdatePublisherReputationAsync(int publisherId, int rating, bool isAdd)
    {
        var reputation = await _unitOfWork.PublisherReputations.GetByPublisherIdAsync(publisherId);

        if (reputation == null)
        {
            reputation = new PublisherReputation
            {
                PublisherId = publisherId,
                AverageRating = rating,
                TotalReviewCount = 1,
                NormalReviewCount = 1,
                ReputationScore = CalculateReputationScore(rating, 1),
                CreatedAt = DateTime.UtcNow,
                LastReviewAt = DateTime.UtcNow
            };
            UpdateStarCounts(reputation, rating, isAdd: true);
            await _unitOfWork.PublisherReputations.AddAsync(reputation);
        }
        else
        {
            if (isAdd)
            {
                var totalRatingSum = reputation.AverageRating * reputation.TotalReviewCount + rating;
                reputation.TotalReviewCount += 1;
                reputation.NormalReviewCount += 1;
                reputation.AverageRating = Math.Round(
                    totalRatingSum / reputation.TotalReviewCount, 2);
            }
            else
            {
                if (reputation.TotalReviewCount > 1)
                {
                    var totalRatingSum = reputation.AverageRating * reputation.TotalReviewCount - rating;
                    reputation.TotalReviewCount -= 1;
                    reputation.NormalReviewCount = Math.Max(0, reputation.NormalReviewCount - 1);
                    reputation.AverageRating = Math.Round(
                        totalRatingSum / reputation.TotalReviewCount, 2);
                }
                else
                {
                    reputation.TotalReviewCount = 0;
                    reputation.NormalReviewCount = 0;
                    reputation.AverageRating = 0m;
                }
            }

            UpdateStarCounts(reputation, rating, isAdd);
            reputation.ReputationScore = CalculateReputationScore(
                reputation.AverageRating, reputation.TotalReviewCount);
            reputation.LastReviewAt = DateTime.UtcNow;
            reputation.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.PublisherReputations.Update(reputation);
        }

        var publisher = await _unitOfWork.Users.GetByIdAsync(publisherId);
        if (publisher != null)
        {
            publisher.ReputationScore = reputation.ReputationScore;
            publisher.ReceivedReviewCount = reputation.TotalReviewCount;
            _unitOfWork.Users.Update(publisher);
        }
    }

    private static void UpdateStarCounts(PublisherReputation reputation, int rating, bool isAdd)
    {
        var delta = isAdd ? 1 : -1;
        switch (rating)
        {
            case 5: reputation.FiveStarCount = Math.Max(0, reputation.FiveStarCount + delta); break;
            case 4: reputation.FourStarCount = Math.Max(0, reputation.FourStarCount + delta); break;
            case 3: reputation.ThreeStarCount = Math.Max(0, reputation.ThreeStarCount + delta); break;
            case 2: reputation.TwoStarCount = Math.Max(0, reputation.TwoStarCount + delta); break;
            case 1: reputation.OneStarCount = Math.Max(0, reputation.OneStarCount + delta); break;
        }
    }

    private static decimal CalculateReputationScore(decimal averageRating, int reviewCount)
    {
        if (reviewCount < MinReviewsForReputation)
        {
            return BaseReputationScore;
        }

        var ratingScore = (averageRating / 5m) * 70m;

        var volumeWeight = Math.Min(1m, (decimal)reviewCount / 20m);
        var volumeScore = volumeWeight * 30m;

        var score = BaseReputationScore + ratingScore + volumeScore;

        return Math.Max(MinReputationScore, Math.Min(MaxReputationScore, Math.Round(score, 2)));
    }

    private class AntiCheatResult
    {
        public bool IsClean { get; set; }
        public int FlagCode { get; set; }
        public string FlagMessage { get; set; } = string.Empty;

        public static AntiCheatResult Clean() => new() { IsClean = true };
        public static AntiCheatResult Flagged(int code, string message) =>
            new() { IsClean = false, FlagCode = code, FlagMessage = message };
    }
}
