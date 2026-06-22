namespace LeftoverShare.API.DTOs.Reviews;

public class PublisherReputationResponse
{
    public int PublisherId { get; set; }
    public string PublisherName { get; set; } = string.Empty;
    public decimal AverageRating { get; set; }
    public int TotalReviewCount { get; set; }
    public int NormalReviewCount { get; set; }
    public decimal ReputationScore { get; set; }
    public int FiveStarCount { get; set; }
    public int FourStarCount { get; set; }
    public int ThreeStarCount { get; set; }
    public int TwoStarCount { get; set; }
    public int OneStarCount { get; set; }
    public DateTime LastReviewAt { get; set; }
}
