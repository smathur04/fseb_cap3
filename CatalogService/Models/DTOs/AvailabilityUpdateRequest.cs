namespace CatalogService.Models.DTOs;

public class AvailabilityUpdateRequest
{
    public int Delta { get; set; }  // -1 to decrement, +1 to increment
}
