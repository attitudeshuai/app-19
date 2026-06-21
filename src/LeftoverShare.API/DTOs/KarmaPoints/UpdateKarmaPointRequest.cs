namespace LeftoverShare.API.DTOs.KarmaPoints;

public class UpdateKarmaPointRequest
{
    public int Points { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int? RelatedId { get; set; }
}
