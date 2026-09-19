using System.Net.Http.Json;
using ReservationService.Models.DTOs;

namespace ReservationService.Clients;

public class CatalogServiceClient
{
    private readonly HttpClient _client;
    private readonly ILogger<CatalogServiceClient> _logger;

    public CatalogServiceClient(HttpClient client, ILogger<CatalogServiceClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<BookInfoDto?> GetBookAsync(Guid bookId)
    {
        try
        {
            var response = await _client.GetAsync($"/api/catalog/books/{bookId}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("CatalogService returned {StatusCode} for book {BookId}", response.StatusCode, bookId);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<BookInfoDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call CatalogService for book {BookId}", bookId);
            return null;
        }
    }

    public async Task<bool> UpdateAvailabilityAsync(Guid bookId, int delta)
    {
        try
        {
            var payload = new { delta };
            var response = await _client.PutAsJsonAsync($"/api/catalog/books/{bookId}/availability", payload);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("CatalogService UpdateAvailability returned {StatusCode} for book {BookId} delta {Delta}",
                    response.StatusCode, bookId, delta);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update availability for book {BookId} with delta {Delta}", bookId, delta);
            return false;
        }
    }
}
