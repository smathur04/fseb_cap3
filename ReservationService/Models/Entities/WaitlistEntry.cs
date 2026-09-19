using System.ComponentModel.DataAnnotations;
using ReservationService.Models.Enums;

namespace ReservationService.Models.Entities;

public class WaitlistEntry
{
    [Key]
    public Guid WaitlistId { get; set; } = Guid.NewGuid();

    [Required]
    public Guid BookId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public WaitlistStatus Status { get; set; } = WaitlistStatus.Waiting;

    public DateTime JoinedAt { get; set; }

    public DateTime? NotifiedAt { get; set; }

    public DateTime? ClaimDeadline { get; set; }

    public Guid? ResultingReservationId { get; set; }

    [Required]
    [MaxLength(255)]
    public string BookTitle { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string BookAuthor { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
