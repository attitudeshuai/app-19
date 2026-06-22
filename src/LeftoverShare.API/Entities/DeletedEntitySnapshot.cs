using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeftoverShare.API.Entities;

public class DeletedEntitySnapshot
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;

    [Required]
    public int EntityId { get; set; }

    [Required]
    [MaxLength(500)]
    public string EntityDisplayName { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "text")]
    public string SnapshotData { get; set; } = string.Empty;

    [Required]
    public int DeletedBy { get; set; }

    [Required]
    public DateTime DeletedAt { get; set; }

    [MaxLength(500)]
    public string? DeletionReason { get; set; }

    public int? OriginalOwnerId { get; set; }

    [ForeignKey(nameof(DeletedBy))]
    public virtual User? DeletedByUser { get; set; }
}
