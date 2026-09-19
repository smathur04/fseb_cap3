namespace CatalogService.Models.DTOs;

public class BookSummaryDto
{
    public Guid BookId { get; set; }
    public string Isbn { get; set; } = "";
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string Genre { get; set; } = "";
    public int? PublicationYear { get; set; }
    public string? Description { get; set; }
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
    public string Status { get; set; } = "";  // "AVAILABLE" or "CHECKED_OUT" - computed
}
