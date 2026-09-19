using Microsoft.EntityFrameworkCore;
using CatalogService.Data;
using CatalogService.Models.DTOs;
using CatalogService.Models.Entities;

namespace CatalogService.Services;

public class BookService : IBookService
{
    private readonly CatalogServiceContext _context;

    public BookService(CatalogServiceContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResponse<BookSummaryDto>> GetBooksAsync(BooksQueryParameters query)
    {
        IQueryable<Book> q = _context.Books.AsNoTracking();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(query.Query))
        {
            var searchTerm = query.Query.ToLower();
            q = q.Where(b =>
                EF.Functions.Like(b.Title.ToLower(), $"%{searchTerm}%") ||
                EF.Functions.Like(b.Author.ToLower(), $"%{searchTerm}%"));
        }

        if (!string.IsNullOrWhiteSpace(query.Genre))
        {
            q = q.Where(b => b.Genre == query.Genre);
        }

        if (!string.IsNullOrWhiteSpace(query.Isbn))
        {
            q = q.Where(b => b.Isbn == query.Isbn);
        }

        if (query.AvailableOnly)
        {
            q = q.Where(b => b.AvailableCopies > 0);
        }

        // Apply sorting
        IOrderedQueryable<Book> orderedQuery = query.SortBy?.ToLower() switch
        {
            "author" => query.SortOrder == "desc"
                ? q.OrderByDescending(b => b.Author)
                : q.OrderBy(b => b.Author),
            "publicationyear" => query.SortOrder == "desc"
                ? q.OrderByDescending(b => b.PublicationYear)
                : q.OrderBy(b => b.PublicationYear),
            _ => query.SortOrder == "desc"
                ? q.OrderByDescending(b => b.Title)
                : q.OrderBy(b => b.Title)
        };

        // Count total before pagination
        var totalElements = await orderedQuery.LongCountAsync();

        // Validate and clamp pagination parameters
        var page = query.Page < 0 ? 0 : query.Page;
        var size = query.Size <= 0 ? 20 : query.Size;

        // Paginate
        var books = await orderedQuery
            .Skip(page * size)
            .Take(size)
            .ToListAsync();

        // Map to DTOs
        var content = books.Select(b => new BookSummaryDto
        {
            BookId = b.BookId,
            Isbn = b.Isbn,
            Title = b.Title,
            Author = b.Author,
            Genre = b.Genre ?? "",
            PublicationYear = b.PublicationYear,
            Description = b.Description,
            TotalCopies = b.TotalCopies,
            AvailableCopies = b.AvailableCopies,
            Status = b.AvailableCopies > 0 ? "AVAILABLE" : "CHECKED_OUT"
        }).ToList();

        var totalPages = size > 0 ? (int)Math.Ceiling((double)totalElements / size) : 0;

        return new PaginatedResponse<BookSummaryDto>
        {
            Content = content,
            Page = page,
            Size = size,
            TotalElements = totalElements,
            TotalPages = totalPages,
            Last = (page + 1) >= totalPages
        };
    }

    public async Task<BookDetailDto?> GetBookByIdAsync(Guid bookId)
    {
        var book = await _context.Books
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.BookId == bookId);

        if (book is null)
            return null;

        return new BookDetailDto
        {
            BookId = book.BookId,
            Isbn = book.Isbn,
            Title = book.Title,
            Author = book.Author,
            Genre = book.Genre ?? "",
            PublicationYear = book.PublicationYear,
            Description = book.Description,
            TotalCopies = book.TotalCopies,
            AvailableCopies = book.AvailableCopies,
            Status = book.AvailableCopies > 0 ? "AVAILABLE" : "CHECKED_OUT",
            Publisher = book.Publisher,
            PageCount = book.PageCount,
            Language = book.Language,
            CreatedAt = book.CreatedAt,
            UpdatedAt = book.UpdatedAt
        };
    }

    public async Task<bool> UpdateAvailabilityAsync(Guid bookId, int delta)
    {
        var book = await _context.Books.FirstOrDefaultAsync(b => b.BookId == bookId);

        if (book is null)
            return false;

        var newAvailable = book.AvailableCopies + delta;

        // Guard: don't go below 0 or above TotalCopies
        if (newAvailable < 0)
            newAvailable = 0;
        if (newAvailable > book.TotalCopies)
            newAvailable = book.TotalCopies;

        book.AvailableCopies = newAvailable;
        await _context.SaveChangesAsync();

        return true;
    }
}
