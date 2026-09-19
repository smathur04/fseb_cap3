namespace ReservationService.Models.DTOs;

public class HistoryItemDto
{
    public Guid ReservationId { get; set; }
    public string BookTitle { get; set; } = "";
    public string BookAuthor { get; set; } = "";
    public DateTime ReservedAt { get; set; }
    public DateTime? CheckedOutAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = "";
    public bool WasLate { get; set; }
}
