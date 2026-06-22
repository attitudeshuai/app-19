namespace LeftoverShare.API.Entities;

public interface ISoftDeletable
{
    bool IsDeleted { get; set; }

    DateTime? DeletedAt { get; set; }

    int? DeletedBy { get; set; }

    string? DeletionReason { get; set; }
}
