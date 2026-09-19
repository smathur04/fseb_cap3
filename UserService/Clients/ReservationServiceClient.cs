using System.Net.Http.Json;
using UserService.Models.DTOs;

namespace UserService.Clients;

public class ReservationServiceClient
{
    private readonly HttpClient _client;
    private readonly ILogger<ReservationServiceClient> _logger;

    public ReservationServiceClient(HttpClient client, ILogger<ReservationServiceClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves reservation statistics for a given user.
    /// Returns null if the call fails (graceful degradation — caller should use zeros).
    /// </summary>
    public async Task<ReservationStatisticsDto?> GetStatisticsAsync(Guid userId)
    {
        try
        {
            var response = await _client.GetAsync($"api/reservations/statistics/{userId}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "ReservationService returned {StatusCode} for user {UserId}.",
                    response.StatusCode, userId);
                return null;
            }

            var stats = await response.Content.ReadFromJsonAsync<ReservationStatisticsDto>();
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to fetch reservation statistics for user {UserId}. Returning null (graceful degradation).",
                userId);
            return null;
        }
    }
}
