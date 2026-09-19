using ReservationService.Models.DTOs;

namespace ReservationService.Services;

public interface IWaitlistService
{
    Task<JoinWaitlistResponse> JoinWaitlistAsync(Guid userId, Guid bookId);
    Task<WaitlistResponse> GetMyWaitlistAsync(Guid userId);
    Task LeaveWaitlistAsync(Guid userId, Guid waitlistId);
}
