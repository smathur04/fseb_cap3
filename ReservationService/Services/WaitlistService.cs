using Microsoft.EntityFrameworkCore;
using ReservationService.Clients;
using ReservationService.Data;
using ReservationService.Exceptions;
using ReservationService.Models.DTOs;
using ReservationService.Models.Entities;
using ReservationService.Models.Enums;

namespace ReservationService.Services;

public class WaitlistService : IWaitlistService
{
    private readonly ReservationServiceContext _db;
    private readonly CatalogServiceClient _catalogClient;
    private readonly IWaitlistCascadeService _cascadeService;
    private readonly ILogger<WaitlistService> _logger;

    public WaitlistService(
        ReservationServiceContext db,
        CatalogServiceClient catalogClient,
        IWaitlistCascadeService cascadeService,
        ILogger<WaitlistService> logger)
    {
        _db = db;
        _catalogClient = catalogClient;
        _cascadeService = cascadeService;
        _logger = logger;
    }

    public async Task<JoinWaitlistResponse> JoinWaitlistAsync(Guid userId, Guid bookId)
    {
        // 1. Get book info
        var book = await _catalogClient.GetBookAsync(bookId);
        if (book == null)
            throw new DomainException("BOOK_NOT_FOUND", "Book not found.", 404);

        // 2. If copies are available, user should just reserve - not waitlist
        if (book.AvailableCopies > 0)
        {
            throw new DomainException(
                "BOOK_AVAILABLE",
                "This book has available copies. Please create a reservation instead.",
                400);
        }

        // 3. Check for existing WAITING entry for this user+book
        var existing = await _db.WaitlistEntries
            .AnyAsync(w => w.UserId == userId && w.BookId == bookId && w.Status == WaitlistStatus.Waiting);

        if (existing)
        {
            throw new DomainException(
                "ALREADY_WAITLISTED",
                "You are already on the waitlist for this book.",
                400);
        }

        // 4. Create new waitlist entry
        var now = DateTime.UtcNow;
        var entry = new WaitlistEntry
        {
            BookId = bookId,
            UserId = userId,
            Status = WaitlistStatus.Waiting,
            JoinedAt = now,
            BookTitle = book.Title,
            BookAuthor = book.Author
        };

        _db.WaitlistEntries.Add(entry);
        await _db.SaveChangesAsync();

        // 5. Compute position (count of WAITING entries with JoinedAt <= this entry's JoinedAt)
        var position = await _db.WaitlistEntries
            .CountAsync(w => w.BookId == bookId && w.Status == WaitlistStatus.Waiting
                             && w.JoinedAt <= entry.JoinedAt);

        _logger.LogInformation("User {UserId} joined waitlist for book {BookId} at position {Position}",
            userId, bookId, position);

        return new JoinWaitlistResponse
        {
            WaitlistId = entry.WaitlistId,
            BookId = entry.BookId,
            BookTitle = entry.BookTitle,
            Status = entry.Status.ToString(),
            JoinedAt = entry.JoinedAt,
            Position = position
        };
    }

    public async Task<WaitlistResponse> GetMyWaitlistAsync(Guid userId)
    {
        var activeStatuses = new[] { WaitlistStatus.Waiting, WaitlistStatus.Notified };

        var entries = await _db.WaitlistEntries
            .Where(w => w.UserId == userId && activeStatuses.Contains(w.Status))
            .OrderBy(w => w.JoinedAt)
            .ToListAsync();

        var dtos = new List<WaitlistEntryDto>();

        foreach (var entry in entries)
        {
            var dto = new WaitlistEntryDto
            {
                WaitlistId = entry.WaitlistId,
                BookId = entry.BookId,
                BookTitle = entry.BookTitle,
                BookAuthor = entry.BookAuthor,
                Status = entry.Status.ToString(),
                JoinedAt = entry.JoinedAt
            };

            if (entry.Status == WaitlistStatus.Waiting)
            {
                // Compute position
                var position = await _db.WaitlistEntries
                    .CountAsync(w => w.BookId == entry.BookId && w.Status == WaitlistStatus.Waiting
                                     && w.JoinedAt <= entry.JoinedAt);
                dto.Position = position;
            }
            else if (entry.Status == WaitlistStatus.Notified)
            {
                dto.NotifiedAt = entry.NotifiedAt;
                dto.ClaimDeadline = entry.ClaimDeadline;
            }

            dtos.Add(dto);
        }

        return new WaitlistResponse { Entries = dtos };
    }

    public async Task LeaveWaitlistAsync(Guid userId, Guid waitlistId)
    {
        var entry = await _db.WaitlistEntries
            .FirstOrDefaultAsync(w => w.WaitlistId == waitlistId && w.UserId == userId);

        if (entry == null)
            throw new DomainException("WAITLIST_ENTRY_NOT_FOUND", "Waitlist entry not found.", 404);

        var priorStatus = entry.Status;

        entry.Status = WaitlistStatus.Cancelled;
        await _db.SaveChangesAsync();

        _logger.LogInformation("User {UserId} left waitlist entry {WaitlistId} (was {PriorStatus})",
            userId, waitlistId, priorStatus);

        // If the user was NOTIFIED and had a reserved copy waiting, free that copy via cascade
        if (priorStatus == WaitlistStatus.Notified)
        {
            // Cancel the resulting reservation if it was still Reserved
            if (entry.ResultingReservationId.HasValue)
            {
                var reservation = await _db.Reservations.FindAsync(entry.ResultingReservationId.Value);
                if (reservation != null && reservation.Status == ReservationStatus.Reserved)
                {
                    reservation.Status = ReservationStatus.Cancelled;
                    await _db.SaveChangesAsync();
                }
            }

            await _cascadeService.CascadeWaitlistAsync(entry.BookId);
        }
    }
}
