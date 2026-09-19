namespace ReservationService.Models.DTOs;

public class UserValidationDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Role { get; set; } = "";
    public string MembershipStatus { get; set; } = "";
    public int ActiveReservationsCount { get; set; }
}
