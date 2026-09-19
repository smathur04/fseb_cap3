namespace ReservationService.Models.DTOs;

public class WaitlistResponse
{
    public List<WaitlistEntryDto> Entries { get; set; } = new();
}
