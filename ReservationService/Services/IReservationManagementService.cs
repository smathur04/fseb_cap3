using ReservationService.Models.DTOs;

namespace ReservationService.Services;

public interface IReservationManagementService
{
    Task<CreateReservationResponse> CreateReservationAsync(Guid userId, Guid bookId);
    Task<ActiveReservationsResponse> GetActiveReservationsAsync(Guid userId);
    Task<CheckoutResponse> CheckoutAsync(Guid reservationId);
    Task<ReturnResponse> ReturnBookAsync(Guid reservationId, string condition, string? notes);
    Task<PaginatedResponse<HistoryItemDto>> GetHistoryAsync(Guid userId, int page, int size);
    Task<ReservationStatisticsDto> GetStatisticsAsync(Guid userId);
}
