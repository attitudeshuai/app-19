namespace LeftoverShare.API.DTOs.Reviews;

public class ReviewLeaderboardEntry
{
    public int PublisherId { get; set; }
    public string PublisherName { get; set; } = string.Empty;
    public decimal ReputationScore { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalReviewCount { get; set; }
    public int Rank { get; set; }
}
