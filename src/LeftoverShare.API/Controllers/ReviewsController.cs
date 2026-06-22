using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LeftoverShare.API.DTOs.Common;
using LeftoverShare.API.DTOs.Reviews;
using LeftoverShare.API.Helpers;
using LeftoverShare.API.Services;

namespace LeftoverShare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;
    private readonly ICurrentUser _currentUser;

    public ReviewsController(IReviewService reviewService, ICurrentUser currentUser)
    {
        _reviewService = reviewService;
        _currentUser = currentUser;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateReviewRequest request)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("用户未认证", 401));
        }

        var reviewerIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var response = await _reviewService.CreateReviewAsync(userId.Value, request, reviewerIp);
        return StatusCode(response.Code, response);
    }

    [HttpGet("publisher/{publisherId}")]
    public async Task<IActionResult> GetByPublisher(int publisherId, [FromQuery] PagedRequest request)
    {
        var response = await _reviewService.GetReviewsByPublisherAsync(publisherId, request);
        return StatusCode(response.Code, response);
    }

    [HttpGet("post/{sharePostId}")]
    public async Task<IActionResult> GetBySharePost(int sharePostId, [FromQuery] PagedRequest request)
    {
        var response = await _reviewService.GetReviewsBySharePostAsync(sharePostId, request);
        return StatusCode(response.Code, response);
    }

    [HttpGet("mine")]
    [Authorize]
    public async Task<IActionResult> GetMyReviews([FromQuery] PagedRequest request)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("用户未认证", 401));
        }

        var response = await _reviewService.GetMyReviewsAsync(userId.Value, request);
        return StatusCode(response.Code, response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var response = await _reviewService.GetReviewByIdAsync(id);
        return StatusCode(response.Code, response);
    }

    [HttpGet("reputation/{publisherId}")]
    public async Task<IActionResult> GetReputation(int publisherId)
    {
        var response = await _reviewService.GetPublisherReputationAsync(publisherId);
        return StatusCode(response.Code, response);
    }

    [HttpGet("aggregation/{publisherId}")]
    public async Task<IActionResult> GetAggregation(int publisherId)
    {
        var response = await _reviewService.GetReviewAggregationAsync(publisherId);
        return StatusCode(response.Code, response);
    }

    [HttpGet("leaderboard")]
    public async Task<IActionResult> GetLeaderboard([FromQuery] int topN = 10)
    {
        var response = await _reviewService.GetLeaderboardAsync(topN);
        return StatusCode(response.Code, response);
    }

    [HttpGet("leaderboard/rating")]
    public async Task<IActionResult> GetLeaderboardByRating([FromQuery] int topN = 10)
    {
        var response = await _reviewService.GetLeaderboardByRatingAsync(topN);
        return StatusCode(response.Code, response);
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("用户未认证", 401));
        }

        var response = await _reviewService.DeleteReviewAsync(id, userId.Value);
        return StatusCode(response.Code, response);
    }
}
