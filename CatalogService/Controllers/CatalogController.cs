using Microsoft.AspNetCore.Mvc;
using CatalogService.Models.DTOs;
using CatalogService.Services;

namespace CatalogService.Controllers;

[ApiController]
[Route("api/catalog")]
public class CatalogController : ControllerBase
{
    private readonly IBookService _bookService;
    private readonly ILogger<CatalogController> _logger;

    public CatalogController(IBookService bookService, ILogger<CatalogController> logger)
    {
        _bookService = bookService;
        _logger = logger;
    }

    /// <summary>
    /// Get a paginated list of books with optional filtering and sorting.
    /// </summary>
    /// <param name="queryParams">Query parameters for filtering, sorting, and pagination.</param>
    /// <returns>Paginated list of books.</returns>
    [HttpGet("books")]
    [ProducesResponseType(typeof(PaginatedResponse<BookSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBooks([FromQuery] BooksQueryParameters queryParams)
    {
        var result = await _bookService.GetBooksAsync(queryParams);
        return Ok(result);
    }

    /// <summary>
    /// Get detailed information about a specific book by its ID.
    /// </summary>
    /// <param name="bookId">The unique identifier of the book.</param>
    /// <returns>Book detail or 404 if not found.</returns>
    [HttpGet("books/{bookId:guid}")]
    [ProducesResponseType(typeof(BookDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBook([FromRoute] Guid bookId)
    {
        var book = await _bookService.GetBookByIdAsync(bookId);

        if (book is null)
        {
            return NotFound(new ErrorResponse
            {
                Error = "NOT_FOUND",
                Message = $"Book not found with ID: {bookId}",
                Timestamp = DateTime.UtcNow
            });
        }

        return Ok(book);
    }

    /// <summary>
    /// Update availability count for a book. Internal endpoint for ReservationService.
    /// </summary>
    /// <param name="bookId">The unique identifier of the book.</param>
    /// <param name="request">Delta to apply to AvailableCopies (-1 to check out, +1 to return).</param>
    /// <returns>200 on success, 404 if book not found.</returns>
    [HttpPut("books/{bookId:guid}/availability")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateAvailability([FromRoute] Guid bookId, [FromBody] AvailabilityUpdateRequest request)
    {
        if (request.Delta != -1 && request.Delta != 1)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "INVALID_DELTA",
                Message = "Delta must be -1 (check out) or +1 (return).",
                Timestamp = DateTime.UtcNow
            });
        }

        var success = await _bookService.UpdateAvailabilityAsync(bookId, request.Delta);

        if (!success)
        {
            return NotFound(new ErrorResponse
            {
                Error = "NOT_FOUND",
                Message = $"Book not found with ID: {bookId}",
                Timestamp = DateTime.UtcNow
            });
        }

        return Ok(new { message = "Availability updated successfully.", bookId });
    }
}
