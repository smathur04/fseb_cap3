using CatalogService.Models.DTOs;

namespace CatalogService.Services;

public interface IBookService
{
    Task<PaginatedResponse<BookSummaryDto>> GetBooksAsync(BooksQueryParameters query);
    Task<BookDetailDto?> GetBookByIdAsync(Guid bookId);
    Task<bool> UpdateAvailabilityAsync(Guid bookId, int delta);
}
