using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeftoverShare.API.Entities;

public class PublisherReputation
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [ForeignKey(nameof(Publisher))]
    public int PublisherId { get; set; }

    public virtual User Publisher { get; set; } = null!;

    [Required]
    [Column(TypeName = "decimal(3, 2)")]
    public decimal AverageRating { get; set; } = 0m;

    [Required]
    public int TotalReviewCount { get; set; } = 0;

    [Required]
    public int NormalReviewCount { get; set; } = 0;

    [Required]
    [Column(TypeName = "decimal(5, 2)")]
    public decimal ReputationScore { get; set; } = 50m;

    [Required]
    public int FiveStarCount { get; set; } = 0;

    [Required]
    public int FourStarCount { get; set; } = 0;

    [Required]
    public int ThreeStarCount { get; set; } = 0;

    [Required]
    public int TwoStarCount { get; set; } = 0;

    [Required]
    public int OneStarCount { get; set; } = 0;

    [Required]
    public DateTime LastReviewAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
