namespace LeftoverShare.API.DTOs.Reviews;

public class ReviewResponse
{
    public int Id { get; set; }
    public int ReservationId { get; set; }
    public int ReviewerId { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public int PublisherId { get; set; }
    public string PublisherName { get; set; } = string.Empty;
    public int SharePostId { get; set; }
    public string SharePostTitle { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
