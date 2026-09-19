using Microsoft.EntityFrameworkCore;
using ReservationService.Data;
using ReservationService.Models.Enums;
using ReservationService.Services;

namespace ReservationService.BackgroundServices;

public class WaitlistExpiryBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WaitlistExpiryBackgroundService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    public WaitlistExpiryBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<WaitlistExpiryBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WaitlistExpiryBackgroundService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredNotificationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in WaitlistExpiryBackgroundService.");
            }

            await Task.Delay(Interval, stoppingToken);
        }

        _logger.LogInformation("WaitlistExpiryBackgroundService stopped.");
    }

    private async Task ProcessExpiredNotificationsAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReservationServiceContext>();
        var cascadeService = scope.ServiceProvider.GetRequiredService<IWaitlistCascadeService>();

        var now = DateTime.UtcNow;

        var expiredEntries = await db.WaitlistEntries
            .Where(w => w.Status == WaitlistStatus.Notified && w.ClaimDeadline < now)
            .ToListAsync(stoppingToken);

        if (expiredEntries.Count == 0)
        {
            _logger.LogDebug("No expired waitlist notifications found.");
            return;
        }

        _logger.LogInformation("Processing {Count} expired waitlist notification(s).", expiredEntries.Count);

        foreach (var entry in expiredEntries)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            try
            {
                entry.Status = WaitlistStatus.Expired;
                await db.SaveChangesAsync(stoppingToken);

                _logger.LogInformation(
                    "Waitlist entry {WaitlistId} for user {UserId} on book {BookId} expired (claim deadline passed). Cascading.",
                    entry.WaitlistId, entry.UserId, entry.BookId);

                // Cancel the resulting reservation if it was still Reserved
                if (entry.ResultingReservationId.HasValue)
                {
                    var reservation = await db.Reservations.FindAsync(
                        new object[] { entry.ResultingReservationId.Value }, stoppingToken);

                    if (reservation != null && reservation.Status == ReservationService.Models.Enums.ReservationStatus.Reserved)
                    {
                        reservation.Status = ReservationService.Models.Enums.ReservationStatus.Cancelled;
                        await db.SaveChangesAsync(stoppingToken);
                    }
                }

                var claimed = await cascadeService.CascadeWaitlistAsync(entry.BookId);

                _logger.LogInformation(
                    "Cascade for book {BookId} after entry {WaitlistId} expiry: copy {Result}.",
                    entry.BookId, entry.WaitlistId, claimed ? "claimed by next patron" : "released to general availability");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process expired waitlist entry {WaitlistId}.", entry.WaitlistId);
            }
        }

        _logger.LogInformation("Finished processing expired waitlist notifications. Total: {Count}.", expiredEntries.Count);
    }
}
