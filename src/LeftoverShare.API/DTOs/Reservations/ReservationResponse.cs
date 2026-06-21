using LeftoverShare.API.DTOs.Auth;
using LeftoverShare.API.DTOs.SharePosts;

namespace LeftoverShare.API.DTOs.Reservations;

public class ReservationResponse
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public SharePostResponse? Post { get; set; }
    public int ClaimerId { get; set; }
    public UserResponse? Claimer { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
