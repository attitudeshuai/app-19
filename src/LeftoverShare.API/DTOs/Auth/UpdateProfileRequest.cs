namespace LeftoverShare.API.DTOs.Auth;

public class UpdateProfileRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Avatar { get; set; }
}
