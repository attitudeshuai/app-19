namespace LeftoverShare.API.DTOs.Auth;

public class UserResponse
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TotalKarmaPoints { get; set; }
}
