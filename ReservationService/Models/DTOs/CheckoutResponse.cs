namespace ReservationService.Models.DTOs;

public class CheckoutResponse
{
    public Guid ReservationId { get; set; }
    public string Status { get; set; } = "";
    public DateTime CheckedOutAt { get; set; }
    public DateTime DueDate { get; set; }
    public string Message { get; set; } = "";
}
