namespace CatalogService.Models.DTOs;

public class ErrorResponse
{
    public string Error { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
