using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LeftoverShare.API.Entities.Enums;

namespace LeftoverShare.API.Entities;

public class Review : ISoftDeletable
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [ForeignKey(nameof(Reservation))]
    public int ReservationId { get; set; }

    public virtual Reservation Reservation { get; set; } = null!;

    [Required]
    [ForeignKey(nameof(Reviewer))]
    public int ReviewerId { get; set; }

    public virtual User Reviewer { get; set; } = null!;

    [Required]
    [ForeignKey(nameof(Publisher))]
    public int PublisherId { get; set; }

    public virtual User Publisher { get; set; } = null!;

    [Required]
    [ForeignKey(nameof(SharePost))]
    public int SharePostId { get; set; }

    public virtual SharePost SharePost { get; set; } = null!;

    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(500)]
    public string? Comment { get; set; }

    [Required]
    public ReviewStatus Status { get; set; } = ReviewStatus.Normal;

    [MaxLength(45)]
    public string? ReviewerIp { get; set; }

    public bool IsFirstReview { get; set; } = false;

    public int? FlagReason { get; set; }

    [MaxLength(500)]
    public string? FlagDetail { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; } = false;

    public DateTime? DeletedAt { get; set; }

    public int? DeletedBy { get; set; }

    [MaxLength(500)]
    public string? DeletionReason { get; set; }

    public int? UniqueGuard { get; set; }
}
