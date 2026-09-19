namespace UserService.Services;

public interface IJwtTokenService
{
    string GenerateToken(Guid userId, string email, string role);
}
