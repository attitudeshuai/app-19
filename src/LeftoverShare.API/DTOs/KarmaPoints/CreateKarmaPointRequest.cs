namespace LeftoverShare.API.DTOs.KarmaPoints;

public class CreateKarmaPointRequest
{
    public int UserId { get; set; }
    public int Points { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int? RelatedId { get; set; }
}
