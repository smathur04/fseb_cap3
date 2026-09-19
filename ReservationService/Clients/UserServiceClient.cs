using System.Net.Http.Json;
using ReservationService.Models.DTOs;

namespace ReservationService.Clients;

public class UserServiceClient
{
    private readonly HttpClient _client;
    private readonly ILogger<UserServiceClient> _logger;

    public UserServiceClient(HttpClient client, ILogger<UserServiceClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<UserValidationDto?> ValidateUserAsync(Guid userId)
    {
        try
        {
            var response = await _client.GetAsync($"/api/users/{userId}/validate");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("UserService returned {StatusCode} for user {UserId}", response.StatusCode, userId);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<UserValidationDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call UserService for user {UserId}", userId);
            return null;
        }
    }
}
