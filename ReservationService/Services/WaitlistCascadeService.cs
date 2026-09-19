using Microsoft.EntityFrameworkCore;
using ReservationService.Clients;
using ReservationService.Data;
using ReservationService.Models.Entities;
using ReservationService.Models.Enums;

namespace ReservationService.Services;

public class WaitlistCascadeService : IWaitlistCascadeService
{
    private readonly ReservationServiceContext _db;
    private readonly UserServiceClient _userClient;
    private readonly CatalogServiceClient _catalogClient;
    private readonly ILogger<WaitlistCascadeService> _logger;

    public WaitlistCascadeService(
        ReservationServiceContext db,
        UserServiceClient userClient,
        CatalogServiceClient catalogClient,
        ILogger<WaitlistCascadeService> logger)
    {
        _db = db;
        _userClient = userClient;
        _catalogClient = catalogClient;
        _logger = logger;
    }

    public async Task<bool> CascadeWaitlistAsync(Guid bookId)
    {
        // Get all WAITING entries for this book ordered oldest first
        var waitingEntries = await _db.WaitlistEntries
            .Where(w => w.BookId == bookId && w.Status == WaitlistStatus.Waiting)
            .OrderBy(w => w.JoinedAt)
            .ToListAsync();

        foreach (var entry in waitingEntries)
        {
            // Validate the patron
            var user = await _userClient.ValidateUserAsync(entry.UserId);

            if (user == null || user.ActiveReservationsCount >= 5 ||
                string.Equals(user.MembershipStatus, "Suspended", StringComparison.OrdinalIgnoreCase))
            {
                // Skip ineligible patron: expire their waitlist entry
                entry.Status = WaitlistStatus.Expired;
                await _db.SaveChangesAsync();

                _logger.LogInformation(
                    "Waitlist entry {WaitlistId} for user {UserId} expired (ineligible during cascade).",
                    entry.WaitlistId, entry.UserId);

                continue;
            }

            // Eligible patron found: create reservation and notify
            var now = DateTime.UtcNow;

            var newReservation = new Reservation
            {
                BookId = bookId,
                UserId = entry.UserId,
                Status = ReservationStatus.Reserved,
                ReservedAt = now,
                ExpiresAt = now.AddDays(7),
                BookTitle = entry.BookTitle,
                BookAuthor = entry.BookAuthor
            };

            _db.Reservations.Add(newReservation);

            entry.Status = WaitlistStatus.Notified;
            entry.NotifiedAt = now;
            entry.ClaimDeadline = now.AddHours(48);
            entry.ResultingReservationId = newReservation.ReservationId;

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Waitlist entry {WaitlistId} notified. New reservation {ReservationId} created for user {UserId}.",
                entry.WaitlistId, newReservation.ReservationId, entry.UserId);

            // Copy was claimed — do NOT increment availability
            return true;
        }

        // No eligible patron found — release the copy to general availability
        await _catalogClient.UpdateAvailabilityAsync(bookId, +1);

        _logger.LogInformation(
            "No eligible waitlist patron for book {BookId}. Availability incremented.", bookId);

        return false;
    }
}
