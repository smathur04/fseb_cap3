namespace ReservationService.Models.DTOs;

public class ReturnRequest
{
    public string Condition { get; set; } = "Good"; // Good, Fair, Poor, Damaged
    public string? Notes { get; set; }
}
