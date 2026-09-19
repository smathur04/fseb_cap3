namespace ReservationService.Models.DTOs;

public class JoinWaitlistResponse
{
    public Guid WaitlistId { get; set; }
    public Guid BookId { get; set; }
    public string BookTitle { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime JoinedAt { get; set; }
    public int Position { get; set; }
}
