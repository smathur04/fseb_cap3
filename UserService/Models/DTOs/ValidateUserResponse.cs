namespace UserService.Models.DTOs;

public class ValidateUserResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Role { get; set; } = "";
    public string MembershipStatus { get; set; } = "";
    public int ActiveReservationsCount { get; set; }
}
