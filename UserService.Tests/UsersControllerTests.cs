using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using UserService.Models.DTOs;
using Xunit;

namespace UserService.Tests;

public class UsersControllerTests : IClassFixture<UserServiceWebApplicationFactory>
{
    private readonly UserServiceWebApplicationFactory _factory;

    public UsersControllerTests(UserServiceWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ─── Helper: register + login, return access token ───────────────────────

    private async Task<string> RegisterAndLoginAsync(string uniqueSuffix)
    {
        var email = $"users_test_{uniqueSuffix}@example.com";
        var client = _factory.CreateClient();

        var registerPayload = new RegisterRequest
        {
            Email = email,
            Password = "StrongPass1!",
            FirstName = "Profile",
            LastName = "User",
            PhoneNumber = "5551110000"
        };
        await client.PostAsJsonAsync("/api/auth/register", registerPayload);

        var loginPayload = new LoginRequest { Email = email, Password = "StrongPass1!" };
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginPayload);
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        return loginBody!.AccessToken;
    }

    // ─── GET /api/users/profile ───────────────────────────────────────────────

    [Fact]
    public async Task GetProfile_WithValidToken_Returns200()
    {
        var token = await RegisterAndLoginAsync("getprofile_valid");

        // Create a fresh client with the Authorization header
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/users/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ProfileResponse>();
        Assert.NotNull(body);
        Assert.Contains("users_test_getprofile_valid@example.com", body.Email);
        Assert.Equal("Patron", body.Role);
        Assert.Equal("Active", body.MembershipStatus);
    }

    [Fact]
    public async Task GetProfile_WithoutToken_Returns401()
    {
        // No Authorization header — should get 401
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/users/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ─── GET /api/users/{userId}/validate ────────────────────────────────────

    [Fact]
    public async Task ValidateUser_WithExistingUser_Returns200()
    {
        var client = _factory.CreateClient();

        // Register a user
        var email = "validate_user_unique@example.com";
        var registerPayload = new RegisterRequest
        {
            Email = email,
            Password = "StrongPass1!",
            FirstName = "Val",
            LastName = "User",
            PhoneNumber = "5550002222"
        };
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registerPayload);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var registerBody = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(registerBody);
        var userId = registerBody.UserId;

        // Call the internal validate endpoint (no auth needed)
        var response = await client.GetAsync($"/api/users/{userId}/validate");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ValidateUserResponse>();
        Assert.NotNull(body);
        Assert.Equal(userId, body.UserId);
        Assert.Equal(email, body.Email);
        Assert.Equal("Active", body.MembershipStatus);
        Assert.True(body.ActiveReservationsCount >= 0);
    }
}
