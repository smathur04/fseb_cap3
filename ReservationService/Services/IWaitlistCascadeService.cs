namespace ReservationService.Services;

public interface IWaitlistCascadeService
{
    /// <summary>
    /// Called when a book copy is freed (on return, on notified entry expiry, on notified entry cancellation).
    /// </summary>
    /// <returns>true if a copy was claimed by a waitlist patron, false if released to general availability.</returns>
    Task<bool> CascadeWaitlistAsync(Guid bookId);
}
