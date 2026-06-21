namespace LeftoverShare.API.DTOs.Reservations;

public class CreateReservationRequest
{
    public int PostId { get; set; }
    public string? Note { get; set; }
}
