using LeftoverShare.API.DTOs.Auth;

namespace LeftoverShare.API.DTOs.KarmaPoints;

public class KarmaPointResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public UserResponse? User { get; set; }
    public int Points { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int? RelatedId { get; set; }
    public DateTime CreatedAt { get; set; }
}
