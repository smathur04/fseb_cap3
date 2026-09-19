namespace ReservationService.Models.DTOs;

public class WaitlistEntryDto
{
    public Guid WaitlistId { get; set; }
    public Guid BookId { get; set; }
    public string BookTitle { get; set; } = "";
    public string BookAuthor { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime JoinedAt { get; set; }
    public int? Position { get; set; }          // only for WAITING
    public DateTime? NotifiedAt { get; set; }    // only for NOTIFIED
    public DateTime? ClaimDeadline { get; set; } // only for NOTIFIED
}
