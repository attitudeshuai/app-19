namespace LeftoverShare.API.DTOs.RecycleBin;

/// <summary>
/// 快照详情响应DTO
/// </summary>
public class SnapshotDetailResponse
{
    public int SnapshotId { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public int EntityId { get; set; }

    public string EntityDisplayName { get; set; } = string.Empty;

    public int DeletedBy { get; set; }

    public string? DeletedByUsername { get; set; }

    public DateTime DeletedAt { get; set; }

    public string? DeletionReason { get; set; }

    public int? OriginalOwnerId { get; set; }

    public Dictionary<string, object?> SnapshotData { get; set; } = new();
}
