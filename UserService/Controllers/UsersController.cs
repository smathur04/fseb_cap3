using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Exceptions;
using UserService.Models.DTOs;
using UserService.Services;

namespace UserService.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserManagementService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserManagementService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>Get the authenticated user's profile including reservation statistics.</summary>
    /// <response code="200">Profile returned.</response>
    /// <response code="401">Token missing or invalid.</response>
    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile()
    {
        // Extract userId from the JWT "sub" claim, falling back to NameIdentifier
        var userIdStr = User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new ErrorResponse(
                "UNAUTHORIZED",
                "Could not identify user from token."));
        }

        try
        {
            var profile = await _userService.GetProfileAsync(userId);
            return Ok(profile);
        }
        catch (AppException ex) when (ex.StatusCode == 404)
        {
            return NotFound(new ErrorResponse(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching profile for {UserId}", userId);
            return StatusCode(500, new ErrorResponse("INTERNAL_ERROR", "An unexpected error occurred."));
        }
    }

    /// <summary>
    /// Validate a user for the ReservationService — confirms the user exists,
    /// has active membership, and returns their current reservation statistics.
    /// </summary>
    /// <response code="200">User is valid.</response>
    /// <response code="403">Membership is not active.</response>
    /// <response code="404">User not found.</response>
    [HttpGet("{userId:guid}/validate")]
    [ProducesResponseType(typeof(ValidateUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ValidateUser(Guid userId)
    {
        try
        {
            var result = await _userService.ValidateUserAsync(userId);
            return Ok(result);
        }
        catch (AppException ex) when (ex.StatusCode == 404)
        {
            return NotFound(new ErrorResponse(ex.ErrorCode, ex.Message));
        }
        catch (AppException ex) when (ex.StatusCode == 403)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new ErrorResponse(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error validating user {UserId}", userId);
            return StatusCode(500, new ErrorResponse("INTERNAL_ERROR", "An unexpected error occurred."));
        }
    }
}
