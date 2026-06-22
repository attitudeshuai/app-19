using System.ComponentModel.DataAnnotations;

namespace LeftoverShare.API.DTOs.RecycleBin;

public class RestoreRequest
{
    [Required]
    public int Id { get; set; }

    [Required]
    [RegularExpression("^(SharePost|Reservation|PickupCode|KarmaPoint)$", ErrorMessage = "EntityType 必须是 SharePost、Reservation、PickupCode 或 KarmaPoint")]
    public string EntityType { get; set; } = string.Empty;
}
