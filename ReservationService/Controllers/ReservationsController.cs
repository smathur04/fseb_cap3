using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReservationService.Exceptions;
using ReservationService.Models.DTOs;
using ReservationService.Services;

namespace ReservationService.Controllers;

[ApiController]
[Route("api/reservations")]
public class ReservationsController : ControllerBase
{
    private readonly IReservationManagementService _reservationService;
    private readonly IWaitlistService _waitlistService;
    private readonly ILogger<ReservationsController> _logger;

    public ReservationsController(
        IReservationManagementService reservationService,
        IWaitlistService waitlistService,
        ILogger<ReservationsController> logger)
    {
        _reservationService = reservationService;
        _waitlistService = waitlistService;
        _logger = logger;
    }

    // -----------------------------------------------------------------------
    // POST /api/reservations
    // -----------------------------------------------------------------------
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateReservation([FromBody] CreateReservationRequest request)
    {
        try
        {
            var userId = GetUserId();
            var result = await _reservationService.CreateReservationAsync(userId, request.BookId);
            return StatusCode(201, result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new ErrorResponse { Error = "UNAUTHORIZED", Message = "Authentication required." });
        }
        catch (DomainException ex)
        {
            return BuildDomainExceptionResponse(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating reservation.");
            return StatusCode(500, new ErrorResponse { Error = "INTERNAL_ERROR", Message = "An unexpected error occurred." });
        }
    }

    // -----------------------------------------------------------------------
    // GET /api/reservations
    // -----------------------------------------------------------------------
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetActiveReservations()
    {
        try
        {
            var userId = GetUserId();
            var result = await _reservationService.GetActiveReservationsAsync(userId);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new ErrorResponse { Error = "UNAUTHORIZED", Message = "Authentication required." });
        }
        catch (DomainException ex)
        {
            return BuildDomainExceptionResponse(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting active reservations.");
            return StatusCode(500, new ErrorResponse { Error = "INTERNAL_ERROR", Message = "An unexpected error occurred." });
        }
    }

    // -----------------------------------------------------------------------
    // GET /api/reservations/history  (literal beats {reservationId})
    // -----------------------------------------------------------------------
    [HttpGet("history")]
    [Authorize]
    public async Task<IActionResult> GetHistory([FromQuery] int page = 0, [FromQuery] int size = 20)
    {
        try
        {
            var userId = GetUserId();
            var result = await _reservationService.GetHistoryAsync(userId, page, size);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new ErrorResponse { Error = "UNAUTHORIZED", Message = "Authentication required." });
        }
        catch (DomainException ex)
        {
            return BuildDomainExceptionResponse(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting reservation history.");
            return StatusCode(500, new ErrorResponse { Error = "INTERNAL_ERROR", Message = "An unexpected error occurred." });
        }
    }

    // -----------------------------------------------------------------------
    // GET /api/reservations/statistics/{userId}  (INTERNAL - no auth required)
    // -----------------------------------------------------------------------
    [HttpGet("statistics/{userId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetStatistics(Guid userId)
    {
        try
        {
            var result = await _reservationService.GetStatisticsAsync(userId);
            return Ok(result);
        }
        catch (DomainException ex)
        {
            return BuildDomainExceptionResponse(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting statistics for user {UserId}.", userId);
            return StatusCode(500, new ErrorResponse { Error = "INTERNAL_ERROR", Message = "An unexpected error occurred." });
        }
    }

    // -----------------------------------------------------------------------
    // POST /api/reservations/waitlist
    // -----------------------------------------------------------------------
    [HttpPost("waitlist")]
    [Authorize]
    public async Task<IActionResult> JoinWaitlist([FromBody] JoinWaitlistRequest request)
    {
        try
        {
            var userId = GetUserId();
            var result = await _waitlistService.JoinWaitlistAsync(userId, request.BookId);
            return StatusCode(201, result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new ErrorResponse { Error = "UNAUTHORIZED", Message = "Authentication required." });
        }
        catch (DomainException ex)
        {
            return BuildDomainExceptionResponse(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error joining waitlist.");
            return StatusCode(500, new ErrorResponse { Error = "INTERNAL_ERROR", Message = "An unexpected error occurred." });
        }
    }

    // -----------------------------------------------------------------------
    // GET /api/reservations/waitlist
    // -----------------------------------------------------------------------
    [HttpGet("waitlist")]
    [Authorize]
    public async Task<IActionResult> GetMyWaitlist()
    {
        try
        {
            var userId = GetUserId();
            var result = await _waitlistService.GetMyWaitlistAsync(userId);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new ErrorResponse { Error = "UNAUTHORIZED", Message = "Authentication required." });
        }
        catch (DomainException ex)
        {
            return BuildDomainExceptionResponse(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting waitlist.");
            return StatusCode(500, new ErrorResponse { Error = "INTERNAL_ERROR", Message = "An unexpected error occurred." });
        }
    }

    // -----------------------------------------------------------------------
    // DELETE /api/reservations/waitlist/{waitlistId}
    // -----------------------------------------------------------------------
    [HttpDelete("waitlist/{waitlistId:guid}")]
    [Authorize]
    public async Task<IActionResult> LeaveWaitlist(Guid waitlistId)
    {
        try
        {
            var userId = GetUserId();
            await _waitlistService.LeaveWaitlistAsync(userId, waitlistId);
            return Ok(new { message = "Successfully removed from waitlist." });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new ErrorResponse { Error = "UNAUTHORIZED", Message = "Authentication required." });
        }
        catch (DomainException ex)
        {
            return BuildDomainExceptionResponse(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error leaving waitlist entry {WaitlistId}.", waitlistId);
            return StatusCode(500, new ErrorResponse { Error = "INTERNAL_ERROR", Message = "An unexpected error occurred." });
        }
    }

    // -----------------------------------------------------------------------
    // POST /api/reservations/{reservationId}/checkout  [Librarian only]
    // -----------------------------------------------------------------------
    [HttpPost("{reservationId:guid}/checkout")]
    [Authorize(Roles = "Librarian")]
    public async Task<IActionResult> Checkout(Guid reservationId, [FromBody] CheckoutRequest? request)
    {
        try
        {
            var result = await _reservationService.CheckoutAsync(reservationId);
            return Ok(result);
        }
        catch (DomainException ex)
        {
            return BuildDomainExceptionResponse(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during checkout of reservation {ReservationId}.", reservationId);
            return StatusCode(500, new ErrorResponse { Error = "INTERNAL_ERROR", Message = "An unexpected error occurred." });
        }
    }

    // -----------------------------------------------------------------------
    // POST /api/reservations/{reservationId}/return  [Librarian only]
    // -----------------------------------------------------------------------
    [HttpPost("{reservationId:guid}/return")]
    [Authorize(Roles = "Librarian")]
    public async Task<IActionResult> ReturnBook(Guid reservationId, [FromBody] ReturnRequest? request)
    {
        try
        {
            var condition = request?.Condition ?? "Good";
            var notes = request?.Notes;
            var result = await _reservationService.ReturnBookAsync(reservationId, condition, notes);
            return Ok(result);
        }
        catch (DomainException ex)
        {
            return BuildDomainExceptionResponse(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error returning reservation {ReservationId}.", reservationId);
            return StatusCode(500, new ErrorResponse { Error = "INTERNAL_ERROR", Message = "An unexpected error occurred." });
        }
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private Guid GetUserId()
    {
        var userIdStr = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdStr, out var id) ? id : throw new UnauthorizedAccessException();
    }

    private IActionResult BuildDomainExceptionResponse(DomainException ex)
    {
        if (ex.ExtraData != null)
        {
            // Merge error/message into extra data object
            var baseProps = new Dictionary<string, object>
            {
                ["error"] = ex.ErrorCode,
                ["message"] = ex.Message,
                ["timestamp"] = DateTime.UtcNow
            };

            // Serialize extra data properties into the response using anonymous object merging via dynamic
            var extraType = ex.ExtraData.GetType();
            var extraProperties = extraType.GetProperties();
            foreach (var prop in extraProperties)
            {
                baseProps[prop.Name] = prop.GetValue(ex.ExtraData)!;
            }

            return StatusCode(ex.StatusCode, baseProps);
        }

        return StatusCode(ex.StatusCode, new
        {
            error = ex.ErrorCode,
            message = ex.Message,
            timestamp = DateTime.UtcNow
        });
    }
}
