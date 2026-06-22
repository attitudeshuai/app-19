namespace LeftoverShare.API.DTOs.RecycleBin;

public class RecycleBinItemResponse
{
    public int Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string EntityDisplayName { get; set; } = string.Empty;
    public string? SnapshotDataPreview { get; set; }
    public int? DeletedBy { get; set; }
    public DateTime DeletedAt { get; set; }
    public string? DeletionReason { get; set; }
    public int? OriginalOwnerId { get; set; }
}
