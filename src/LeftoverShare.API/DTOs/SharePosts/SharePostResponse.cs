using LeftoverShare.API.DTOs.Auth;

namespace LeftoverShare.API.DTOs.SharePosts;

public class SharePostResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string FoodType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string PickupAddress { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime AvailableUntil { get; set; }
    public List<string>? Photos { get; set; }
    public string Status { get; set; } = string.Empty;
    public int PosterId { get; set; }
    public UserResponse? Poster { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
