using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UserService.Data;
using UserService.Models.DTOs;
using Xunit;

namespace UserService.Tests;

/// <summary>
/// Custom WebApplicationFactory that replaces the database with a unique InMemory store
/// and wires the ReservationServiceClient to a no-op mock (tests don't need that service).
/// </summary>
public class UserServiceWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"UserTestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Use Development so Program.cs picks InMemory path (not Npgsql)
        builder.UseEnvironment("Development");

        // ConfigureTestServices runs AFTER the app's services are registered
        builder.ConfigureTestServices(services =>
        {
            // ── Replace DB ──────────────────────────────────────────────────
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<UserServiceContext>));
            if (dbDescriptor != null) services.Remove(dbDescriptor);

            services.AddDbContext<UserServiceContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            // ── Mock ReservationServiceClient ───────────────────────────────
            // Profile endpoint calls ReservationService to get statistics.
            // In tests, we return null (graceful degradation).
            var resClientDesc = services.SingleOrDefault(
                d => d.ServiceType == typeof(UserService.Clients.ReservationServiceClient));
            if (resClientDesc != null) services.Remove(resClientDesc);

            services.AddHttpClient<UserService.Clients.ReservationServiceClient>(c =>
                c.BaseAddress = new Uri("http://mock-reservation:9999"));
        });
    }
}

public class AuthControllerTests : IClassFixture<UserServiceWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(UserServiceWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static RegisterRequest ValidRegisterPayload(string email) => new()
    {
        Email = email,
        Password = "StrongPass1!",
        FirstName = "John",
        LastName = "Doe",
        PhoneNumber = "5551234567"
    };

    // ─── POST /api/auth/register ──────────────────────────────────────────────

    [Fact]
    public async Task Register_WithValidData_Returns201()
    {
        var payload = ValidRegisterPayload("valid_register@example.com");

        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.UserId);
        Assert.Equal(payload.Email, body.Email);
        Assert.Equal("Patron", body.Role);
        Assert.Equal("Active", body.MembershipStatus);
        Assert.NotEmpty(body.Message);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns400()
    {
        var payload = ValidRegisterPayload("duplicate@example.com");

        // First registration — should succeed
        var first = await _client.PostAsJsonAsync("/api/auth/register", payload);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        // Second registration with same email — should fail
        var second = await _client.PostAsJsonAsync("/api/auth/register", payload);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);

        var body = await second.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("VALIDATION_ERROR", body.Error);
    }

    [Fact]
    public async Task Register_WithWeakPassword_Returns400()
    {
        var payload = new RegisterRequest
        {
            Email = "weakpass@example.com",
            Password = "weak",           // too short, no uppercase/digits/special chars
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "5551234567"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("VALIDATION_ERROR", body.Error);
    }

    [Fact]
    public async Task Register_WithMissingFields_Returns400()
    {
        // Email field is missing
        var payload = new
        {
            Password = "StrongPass1!",
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "5551234567"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── POST /api/auth/login ─────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithValidCredentials_Returns200WithToken()
    {
        var email = "login_valid@example.com";
        const string password = "StrongPass1!";

        // Register first
        var registerPayload = new RegisterRequest
        {
            Email = email,
            Password = password,
            FirstName = "Jane",
            LastName = "Smith",
            PhoneNumber = "5559876543"
        };
        var registerResp = await _client.PostAsJsonAsync("/api/auth/register", registerPayload);
        Assert.Equal(HttpStatusCode.Created, registerResp.StatusCode);

        // Login
        var loginPayload = new LoginRequest { Email = email, Password = password };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginPayload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);
        Assert.NotEmpty(body.AccessToken);
        Assert.Equal("Bearer", body.TokenType);
        Assert.True(body.ExpiresIn > 0);
        Assert.Equal(email, body.User.Email);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var email = "login_wrong_pass@example.com";

        // Register
        var registerPayload = new RegisterRequest
        {
            Email = email,
            Password = "StrongPass1!",
            FirstName = "Tom",
            LastName = "Hardy",
            PhoneNumber = "5550001111"
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerPayload);

        // Login with wrong password
        var loginPayload = new LoginRequest { Email = email, Password = "WrongPass99!" };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginPayload);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("AUTHENTICATION_FAILED", body.Error);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        var loginPayload = new LoginRequest
        {
            Email = "nobody@nowhere.com",
            Password = "StrongPass1!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", loginPayload);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("AUTHENTICATION_FAILED", body.Error);
    }
}
