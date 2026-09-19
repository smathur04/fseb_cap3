namespace CatalogService.Models.DTOs;

public class BooksQueryParameters
{
    public int Page { get; set; } = 0;
    public int Size { get; set; } = 20;
    public string SortBy { get; set; } = "title";      // title, author, publicationYear
    public string SortOrder { get; set; } = "asc";     // asc, desc
    public string? Query { get; set; }
    public string? Genre { get; set; }
    public string? Isbn { get; set; }
    public bool AvailableOnly { get; set; } = false;
}
