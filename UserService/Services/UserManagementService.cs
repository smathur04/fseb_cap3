using Microsoft.EntityFrameworkCore;
using UserService.Clients;
using UserService.Data;
using UserService.Exceptions;
using UserService.Models.DTOs;
using UserService.Models.Entities;
using UserService.Models.Enums;

namespace UserService.Services;

public class UserManagementService : IUserManagementService
{
    private readonly UserServiceContext _db;
    private readonly IPasswordService _passwordService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ReservationServiceClient _reservationClient;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(
        UserServiceContext db,
        IPasswordService passwordService,
        IJwtTokenService jwtTokenService,
        ReservationServiceClient reservationClient,
        ILogger<UserManagementService> logger)
    {
        _db = db;
        _passwordService = passwordService;
        _jwtTokenService = jwtTokenService;
        _reservationClient = reservationClient;
        _logger = logger;
    }

    // ─── Register ──────────────────────────────────────────────────────────────

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        // Check email uniqueness
        var emailTaken = await _db.Users
            .AnyAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (emailTaken)
            throw AppException.ValidationError("Email already exists");

        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = request.Email.Trim().ToLower(),
            PasswordHash = _passwordService.HashPassword(request.Password),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            Role = Role.Patron,
            MembershipStatus = MembershipStatus.Active
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _logger.LogInformation("New user registered: {UserId} ({Email})", user.UserId, user.Email);

        return new RegisterResponse
        {
            UserId = user.UserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role.ToString(),
            MembershipStatus = user.MembershipStatus.ToString(),
            CreatedAt = user.CreatedAt,
            Message = "User registered successfully."
        };
    }

    // ─── Login ─────────────────────────────────────────────────────────────────

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (user is null || !_passwordService.VerifyPassword(request.Password, user.PasswordHash))
            throw AppException.AuthenticationFailed();

        var token = _jwtTokenService.GenerateToken(user.UserId, user.Email, user.Role.ToString());

        _logger.LogInformation("User logged in: {UserId} ({Email})", user.UserId, user.Email);

        return new LoginResponse
        {
            AccessToken = token,
            TokenType = "Bearer",
            ExpiresIn = 86400,
            User = new UserDto
            {
                UserId = user.UserId,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role.ToString()
            }
        };
    }

    // ─── GetProfile ────────────────────────────────────────────────────────────

    public async Task<ProfileResponse> GetProfileAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw AppException.NotFound($"User {userId} not found.");

        // Graceful degradation: use zeros if ReservationService is unavailable
        var stats = await _reservationClient.GetStatisticsAsync(userId);

        return new ProfileResponse
        {
            UserId = user.UserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber ?? "",
            Role = user.Role.ToString(),
            MembershipStatus = user.MembershipStatus.ToString(),
            MemberSince = user.MemberSince,
            ActiveReservations = stats?.ActiveReservations ?? 0,
            BorrowingHistory = stats?.BorrowingHistory ?? 0
        };
    }

    // ─── ValidateUser ──────────────────────────────────────────────────────────

    public async Task<ValidateUserResponse> ValidateUserAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw AppException.NotFound($"User {userId} not found.");

        if (user.MembershipStatus != MembershipStatus.Active)
            throw AppException.MembershipInactive(
                $"User membership is {user.MembershipStatus}. Active membership required.");

        var stats = await _reservationClient.GetStatisticsAsync(userId);

        return new ValidateUserResponse
        {
            UserId = user.UserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role.ToString(),
            MembershipStatus = user.MembershipStatus.ToString(),
            ActiveReservationsCount = stats?.ActiveReservations ?? 0
        };
    }
}
