namespace LeftoverShare.API.DTOs.SharePosts;

public class SharePostListResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string FoodType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string PickupAddress { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime AvailableUntil { get; set; }
    public string? FirstPhoto { get; set; }
    public string Status { get; set; } = string.Empty;
    public int PosterId { get; set; }
    public string PosterUsername { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
