using Microsoft.EntityFrameworkCore;
using ReservationService.Clients;
using ReservationService.Data;
using ReservationService.Exceptions;
using ReservationService.Models.DTOs;
using ReservationService.Models.Entities;
using ReservationService.Models.Enums;

namespace ReservationService.Services;

public class ReservationManagementService : IReservationManagementService
{
    private readonly ReservationServiceContext _db;
    private readonly UserServiceClient _userClient;
    private readonly CatalogServiceClient _catalogClient;
    private readonly IWaitlistCascadeService _cascadeService;
    private readonly ILogger<ReservationManagementService> _logger;

    public ReservationManagementService(
        ReservationServiceContext db,
        UserServiceClient userClient,
        CatalogServiceClient catalogClient,
        IWaitlistCascadeService cascadeService,
        ILogger<ReservationManagementService> logger)
    {
        _db = db;
        _userClient = userClient;
        _catalogClient = catalogClient;
        _cascadeService = cascadeService;
        _logger = logger;
    }

    public async Task<CreateReservationResponse> CreateReservationAsync(Guid userId, Guid bookId)
    {
        // 1. Validate user
        var user = await _userClient.ValidateUserAsync(userId);
        if (user == null)
            throw new DomainException("USER_NOT_FOUND", "User not found or service unavailable.", 404);

        if (string.Equals(user.MembershipStatus, "Suspended", StringComparison.OrdinalIgnoreCase))
            throw new DomainException("USER_SUSPENDED", "Your account is suspended.", 403);

        // 2. Check reservation limit
        if (user.ActiveReservationsCount >= 5)
        {
            throw new DomainException(
                "RESERVATION_LIMIT_EXCEEDED",
                "You have reached the maximum number of active reservations.",
                400,
                new { currentReservations = user.ActiveReservationsCount });
        }

        // 3. Get book info
        var book = await _catalogClient.GetBookAsync(bookId);
        if (book == null)
            throw new DomainException("BOOK_NOT_FOUND", "Book not found.", 404);

        // 4. Check availability
        if (book.AvailableCopies <= 0)
        {
            throw new DomainException(
                "BOOK_UNAVAILABLE",
                "No copies of this book are currently available.",
                400,
                new { availableCopies = 0 });
        }

        // 5. Decrement availability
        await _catalogClient.UpdateAvailabilityAsync(bookId, -1);

        // 6. Create reservation
        var now = DateTime.UtcNow;
        var reservation = new Reservation
        {
            BookId = bookId,
            UserId = userId,
            Status = ReservationStatus.Reserved,
            ReservedAt = now,
            ExpiresAt = now.AddDays(7),
            BookTitle = book.Title,
            BookAuthor = book.Author
        };

        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Reservation {ReservationId} created for user {UserId} on book {BookId}",
            reservation.ReservationId, userId, bookId);

        return new CreateReservationResponse
        {
            ReservationId = reservation.ReservationId,
            BookId = reservation.BookId,
            UserId = reservation.UserId,
            BookTitle = reservation.BookTitle,
            Status = reservation.Status.ToString(),
            ReservedAt = reservation.ReservedAt,
            ExpiresAt = reservation.ExpiresAt,
            Message = "Reservation created successfully."
        };
    }

    public async Task<ActiveReservationsResponse> GetActiveReservationsAsync(Guid userId)
    {
        var activeStatuses = new[] { ReservationStatus.Reserved, ReservationStatus.CheckedOut };

        var reservations = await _db.Reservations
            .Where(r => r.UserId == userId && activeStatuses.Contains(r.Status))
            .OrderBy(r => r.ReservedAt)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var dtos = reservations.Select(r =>
        {
            var dto = new ActiveReservationDto
            {
                ReservationId = r.ReservationId,
                BookId = r.BookId,
                BookTitle = r.BookTitle,
                BookAuthor = r.BookAuthor,
                Status = r.Status.ToString(),
                ReservedAt = r.ReservedAt,
                ExpiresAt = r.ExpiresAt,
                CheckedOutAt = r.CheckedOutAt,
                DueDate = r.DueDate
            };

            if (r.Status == ReservationStatus.Reserved && r.ExpiresAt.HasValue)
            {
                var days = (int)Math.Ceiling((r.ExpiresAt.Value - now).TotalDays);
                dto.DaysUntilExpiry = Math.Max(0, days);
            }

            if (r.Status == ReservationStatus.CheckedOut && r.DueDate.HasValue)
            {
                var days = (int)Math.Ceiling((r.DueDate.Value - now).TotalDays);
                dto.DaysUntilDue = days; // can be negative (overdue)
            }

            return dto;
        }).ToList();

        return new ActiveReservationsResponse
        {
            Reservations = dtos,
            TotalActive = dtos.Count
        };
    }

    public async Task<CheckoutResponse> CheckoutAsync(Guid reservationId)
    {
        var reservation = await _db.Reservations.FindAsync(reservationId);
        if (reservation == null)
            throw new DomainException("RESERVATION_NOT_FOUND", "Reservation not found.", 404);

        if (reservation.Status != ReservationStatus.Reserved)
        {
            throw new DomainException(
                "INVALID_STATUS",
                $"Reservation cannot be checked out in its current status.",
                400,
                new { currentStatus = reservation.Status.ToString() });
        }

        var now = DateTime.UtcNow;
        reservation.Status = ReservationStatus.CheckedOut;
        reservation.CheckedOutAt = now;
        reservation.DueDate = now.AddDays(14);

        await _db.SaveChangesAsync();

        _logger.LogInformation("Reservation {ReservationId} checked out. Due: {DueDate}",
            reservation.ReservationId, reservation.DueDate);

        return new CheckoutResponse
        {
            ReservationId = reservation.ReservationId,
            Status = reservation.Status.ToString(),
            CheckedOutAt = reservation.CheckedOutAt!.Value,
            DueDate = reservation.DueDate!.Value,
            Message = "Book checked out successfully."
        };
    }

    public async Task<ReturnResponse> ReturnBookAsync(Guid reservationId, string condition, string? notes)
    {
        var reservation = await _db.Reservations.FindAsync(reservationId);
        if (reservation == null)
            throw new DomainException("RESERVATION_NOT_FOUND", "Reservation not found.", 404);

        if (reservation.Status != ReservationStatus.CheckedOut)
        {
            throw new DomainException(
                "INVALID_STATUS",
                "Reservation cannot be returned in its current status.",
                400,
                new { currentStatus = reservation.Status.ToString() });
        }

        var now = DateTime.UtcNow;
        var parsedCondition = Enum.TryParse<BookCondition>(condition, true, out var condResult)
            ? condResult
            : BookCondition.Good;

        int lateDays = 0;
        decimal lateFee = 0m;

        if (reservation.DueDate.HasValue && now > reservation.DueDate.Value)
        {
            lateDays = (int)Math.Ceiling((now - reservation.DueDate.Value).TotalDays);
            lateDays = Math.Max(0, lateDays);
            lateFee = lateDays * 1.00m;
        }

        reservation.Status = ReservationStatus.Returned;
        reservation.ReturnedAt = now;
        reservation.Condition = parsedCondition;
        reservation.Notes = notes;
        reservation.LateDays = lateDays;
        reservation.LateFee = lateFee;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Reservation {ReservationId} returned. Late days: {LateDays}, Fee: {LateFee}",
            reservation.ReservationId, lateDays, lateFee);

        // Cascade waitlist - will either assign the copy to a waitlisted patron or increment availability
        await _cascadeService.CascadeWaitlistAsync(reservation.BookId);

        return new ReturnResponse
        {
            ReservationId = reservation.ReservationId,
            ReturnedAt = reservation.ReturnedAt!.Value,
            DueDate = reservation.DueDate,
            LateDays = lateDays,
            LateFee = lateFee,
            Message = lateDays > 0
                ? $"Book returned {lateDays} day(s) late. Late fee: ${lateFee:F2}."
                : "Book returned on time. Thank you!"
        };
    }

    public async Task<PaginatedResponse<HistoryItemDto>> GetHistoryAsync(Guid userId, int page, int size)
    {
        var query = _db.Reservations
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.ReturnedAt ?? r.ReservedAt);

        var totalElements = await query.LongCountAsync();
        var totalPages = (int)Math.Ceiling((double)totalElements / size);

        var items = await query
            .Skip(page * size)
            .Take(size)
            .ToListAsync();

        var content = items.Select(r => new HistoryItemDto
        {
            ReservationId = r.ReservationId,
            BookTitle = r.BookTitle,
            BookAuthor = r.BookAuthor,
            ReservedAt = r.ReservedAt,
            CheckedOutAt = r.CheckedOutAt,
            ReturnedAt = r.ReturnedAt,
            DueDate = r.DueDate,
            Status = r.Status.ToString(),
            WasLate = r.LateDays.HasValue && r.LateDays.Value > 0
        }).ToList();

        return new PaginatedResponse<HistoryItemDto>
        {
            Content = content,
            Page = page,
            Size = size,
            TotalElements = totalElements,
            TotalPages = totalPages,
            Last = (page + 1) >= totalPages
        };
    }

    public async Task<ReservationStatisticsDto> GetStatisticsAsync(Guid userId)
    {
        var activeStatuses = new[] { ReservationStatus.Reserved, ReservationStatus.CheckedOut };

        var activeCount = await _db.Reservations
            .CountAsync(r => r.UserId == userId && activeStatuses.Contains(r.Status));

        var historyCount = await _db.Reservations
            .CountAsync(r => r.UserId == userId && r.Status == ReservationStatus.Returned);

        return new ReservationStatisticsDto
        {
            UserId = userId,
            ActiveReservations = activeCount,
            BorrowingHistory = historyCount
        };
    }
}
