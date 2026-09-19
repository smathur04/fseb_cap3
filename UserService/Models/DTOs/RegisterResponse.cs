namespace UserService.Models.DTOs;

public class RegisterResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Role { get; set; } = "";
    public string MembershipStatus { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string Message { get; set; } = "";
}
