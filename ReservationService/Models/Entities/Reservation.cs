using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ReservationService.Models.Enums;

namespace ReservationService.Models.Entities;

public class Reservation
{
    [Key]
    public Guid ReservationId { get; set; } = Guid.NewGuid();

    [Required]
    public Guid BookId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public ReservationStatus Status { get; set; } = ReservationStatus.Reserved;

    public DateTime ReservedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime? CheckedOutAt { get; set; }

    public DateTime? DueDate { get; set; }

    public DateTime? ReturnedAt { get; set; }

    public int RenewalCount { get; set; } = 0;

    public int? LateDays { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? LateFee { get; set; }

    public BookCondition? Condition { get; set; }

    public string? Notes { get; set; }

    [Required]
    [MaxLength(255)]
    public string BookTitle { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string BookAuthor { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
