using LeftoverShare.API.DTOs.Reservations;

namespace LeftoverShare.API.DTOs.PickupCodes;

public class PickupCodeResponse
{
    public int Id { get; set; }
    public int ReservationId { get; set; }
    public ReservationResponse? Reservation { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
