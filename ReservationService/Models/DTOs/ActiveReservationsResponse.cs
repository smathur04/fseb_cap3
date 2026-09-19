namespace ReservationService.Models.DTOs;

public class ActiveReservationsResponse
{
    public List<ActiveReservationDto> Reservations { get; set; } = new();
    public int TotalActive { get; set; }
}
