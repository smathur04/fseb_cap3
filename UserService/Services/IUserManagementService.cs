using UserService.Models.DTOs;

namespace UserService.Services;

public interface IUserManagementService
{
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<ProfileResponse> GetProfileAsync(Guid userId);
    Task<ValidateUserResponse> ValidateUserAsync(Guid userId);
}
