using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ReservationService.Clients;
using ReservationService.Data;
using ReservationService.Models.DTOs;
using ReservationService.Models.Enums;
using Xunit;

namespace ReservationService.Tests;

// ─── Configurable mock state ─────────────────────────────────────────────────

/// <summary>
/// Singleton held in the test DI container. Tests mutate it before each call
/// to control what the mock HTTP handlers return.
/// </summary>
public class MockServiceState
{
    public string UserMembershipStatus { get; set; } = "Active";
    public int UserActiveReservations { get; set; } = 0;
    public int BookAvailableCopies { get; set; } = 3;
    public string BookTitle { get; set; } = "Test Book";
    public string BookAuthor { get; set; } = "Test Author";
}

// ─── Mock HTTP handler ────────────────────────────────────────────────────────

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

    public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(_handler(request));
}

// ─── Factory ─────────────────────────────────────────────────────────────────

public class ReservationServiceWebApplicationFactory : WebApplicationFactory<Program>
{
    // JWT credentials used both to sign test tokens and to configure the app validator
    internal const string TestJwtSecret = "test-jwt-secret-for-integration-tests-min32!";
    internal const string TestJwtIssuer = "LibraryManagementApi";
    internal const string TestJwtAudience = "LibraryManagementApiUsers";

    private readonly string _dbName = $"ReservationTestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Development avoids the Npgsql connection string requirement.
        builder.UseEnvironment("Development");

        // Override JWT secret and service URLs before Program.cs reads them.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = TestJwtSecret,
                ["Jwt:Issuer"] = TestJwtIssuer,
                ["Jwt:Audience"] = TestJwtAudience,
                ["ServiceUrls:UserService"] = "http://mock-user-service",
                ["ServiceUrls:CatalogService"] = "http://mock-catalog-service"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // ── Replace DB ────────────────────────────────────────────────────
            var dbDesc = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ReservationServiceContext>));
            if (dbDesc != null) services.Remove(dbDesc);

            services.AddDbContext<ReservationServiceContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            // ── Register mock state singleton ─────────────────────────────────
            services.AddSingleton<MockServiceState>();

            // ── Replace UserServiceClient ─────────────────────────────────────
            var userDesc = services.SingleOrDefault(
                d => d.ServiceType == typeof(UserServiceClient));
            if (userDesc != null) services.Remove(userDesc);

            services.AddTransient<UserServiceClient>(sp =>
            {
                var state = sp.GetRequiredService<MockServiceState>();
                var handler = new MockHttpMessageHandler(request =>
                {
                    // Extract userId from URL: /api/users/{id}/validate
                    var pathSegments = request.RequestUri!.AbsolutePath.Split('/');
                    Guid.TryParse(pathSegments.LastOrDefault(s => s != "validate"), out var uid);

                    var dto = new UserValidationDto
                    {
                        UserId = uid == Guid.Empty ? Guid.NewGuid() : uid,
                        Email = "testpatron@example.com",
                        FirstName = "Test",
                        LastName = "Patron",
                        Role = "Patron",
                        MembershipStatus = state.UserMembershipStatus,
                        ActiveReservationsCount = state.UserActiveReservations
                    };
                    var json = JsonSerializer.Serialize(dto,
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                });

                var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://mock-user-service") };
                var logger = sp.GetRequiredService<ILogger<UserServiceClient>>();
                return new UserServiceClient(httpClient, logger);
            });

            // ── Replace CatalogServiceClient ──────────────────────────────────
            var catDesc = services.SingleOrDefault(
                d => d.ServiceType == typeof(CatalogServiceClient));
            if (catDesc != null) services.Remove(catDesc);

            services.AddTransient<CatalogServiceClient>(sp =>
            {
                var state = sp.GetRequiredService<MockServiceState>();
                var handler = new MockHttpMessageHandler(request =>
                {
                    var path = request.RequestUri!.AbsolutePath;

                    // GET /api/catalog/books/{id}
                    if (request.Method == HttpMethod.Get && path.Contains("/api/catalog/books/"))
                    {
                        var segments = path.Split('/');
                        Guid.TryParse(segments[^1], out var bookId);

                        var dto = new BookInfoDto
                        {
                            BookId = bookId == Guid.Empty ? Guid.NewGuid() : bookId,
                            Title = state.BookTitle,
                            Author = state.BookAuthor,
                            AvailableCopies = state.BookAvailableCopies,
                            TotalCopies = 5
                        };
                        var json = JsonSerializer.Serialize(dto,
                            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                        return new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(json, Encoding.UTF8, "application/json")
                        };
                    }

                    // PUT /api/catalog/books/{id}/availability
                    if (request.Method == HttpMethod.Put && path.Contains("/availability"))
                    {
                        return new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent("{}", Encoding.UTF8, "application/json")
                        };
                    }

                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                });

                var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://mock-catalog-service") };
                var logger = sp.GetRequiredService<ILogger<CatalogServiceClient>>();
                return new CatalogServiceClient(httpClient, logger);
            });
        });
    }

    /// <summary>
    /// Generates a JWT signed with the same secret the test app uses to validate tokens.
    /// </summary>
    public string GenerateTestToken(Guid userId, string role = "Patron")
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("sub", userId.ToString()),
            new Claim("role", role),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: TestJwtIssuer,
            audience: TestJwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

// ─── Tests ───────────────────────────────────────────────────────────────────

public class ReservationsControllerTests : IClassFixture<ReservationServiceWebApplicationFactory>
{
    private readonly ReservationServiceWebApplicationFactory _factory;
    private readonly MockServiceState _mockState;

    // Fixed user IDs that map to JWT tokens
    private static readonly Guid PatronId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid LibrarianId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");

    public ReservationsControllerTests(ReservationServiceWebApplicationFactory factory)
    {
        _factory = factory;
        // Resolve the singleton mock state so tests can configure it
        _mockState = factory.Services.GetRequiredService<MockServiceState>();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private HttpClient ClientWithToken(string role = "Patron")
    {
        var userId = role == "Librarian" ? LibrarianId : PatronId;
        var token = _factory.GenerateTestToken(userId, role);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Creates a reservation using the Patron user with the supplied bookId.
    /// Resets mock state to defaults that allow reservation creation.
    /// </summary>
    private async Task<Guid> CreateReservationAsync(Guid? bookId = null)
    {
        _mockState.BookAvailableCopies = 3;
        _mockState.UserActiveReservations = 0;
        _mockState.UserMembershipStatus = "Active";
        _mockState.BookTitle = "Test Book";
        _mockState.BookAuthor = "Test Author";

        var client = ClientWithToken("Patron");
        var response = await client.PostAsJsonAsync("/api/reservations",
            new CreateReservationRequest { BookId = bookId ?? Guid.NewGuid() });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<CreateReservationResponse>();
        return body!.ReservationId;
    }

    // ─── 1. CreateReservation_WithValidToken_Returns201 ───────────────────────

    [Fact]
    public async Task CreateReservation_WithValidToken_Returns201()
    {
        _mockState.BookAvailableCopies = 3;
        _mockState.UserActiveReservations = 0;
        _mockState.UserMembershipStatus = "Active";
        _mockState.BookTitle = "Clean Code";
        _mockState.BookAuthor = "Robert C. Martin";

        var client = ClientWithToken("Patron");
        var bookId = Guid.NewGuid();
        var payload = new CreateReservationRequest { BookId = bookId };

        var response = await client.PostAsJsonAsync("/api/reservations", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CreateReservationResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.ReservationId);
        Assert.Equal(bookId, body.BookId);
        Assert.Equal("Reserved", body.Status);
        Assert.True(body.ExpiresAt > DateTime.UtcNow);
        Assert.NotEmpty(body.Message);
    }

    // ─── 2. CreateReservation_WithoutToken_Returns401 ────────────────────────

    [Fact]
    public async Task CreateReservation_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient(); // no auth header
        var payload = new CreateReservationRequest { BookId = Guid.NewGuid() };

        var response = await client.PostAsJsonAsync("/api/reservations", payload);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ─── 3. GetActiveReservations_Returns200 ─────────────────────────────────

    [Fact]
    public async Task GetActiveReservations_Returns200()
    {
        // Ensure at least one reservation exists for the patron
        await CreateReservationAsync(Guid.NewGuid());

        var client = ClientWithToken("Patron");
        var response = await client.GetAsync("/api/reservations");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ActiveReservationsResponse>();
        Assert.NotNull(body);
        Assert.NotNull(body.Reservations);
        Assert.True(body.TotalActive >= 1);
    }

    // ─── 4. Checkout_AsLibrarian_Returns200 ──────────────────────────────────

    [Fact]
    public async Task Checkout_AsLibrarian_Returns200()
    {
        var reservationId = await CreateReservationAsync(Guid.NewGuid());

        var librarianClient = ClientWithToken("Librarian");
        var response = await librarianClient.PostAsJsonAsync(
            $"/api/reservations/{reservationId}/checkout",
            new CheckoutRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CheckoutResponse>();
        Assert.NotNull(body);
        Assert.Equal(reservationId, body.ReservationId);
        Assert.Equal("CheckedOut", body.Status);
        Assert.True(body.DueDate > DateTime.UtcNow);
        Assert.True(body.CheckedOutAt > DateTime.MinValue);
        Assert.NotEmpty(body.Message);
    }

    // ─── 5. Checkout_AsPatron_Returns403 ─────────────────────────────────────

    [Fact]
    public async Task Checkout_AsPatron_Returns403()
    {
        var reservationId = await CreateReservationAsync(Guid.NewGuid());

        // Patron does NOT have the Librarian role
        var patronClient = ClientWithToken("Patron");
        var response = await patronClient.PostAsJsonAsync(
            $"/api/reservations/{reservationId}/checkout",
            new CheckoutRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ─── 6. Return_AsLibrarian_Returns200 ────────────────────────────────────

    [Fact]
    public async Task Return_AsLibrarian_Returns200()
    {
        var reservationId = await CreateReservationAsync(Guid.NewGuid());

        var librarianClient = ClientWithToken("Librarian");

        // Checkout first
        await librarianClient.PostAsJsonAsync(
            $"/api/reservations/{reservationId}/checkout",
            new CheckoutRequest());

        // Return (book returned on time — DueDate is 14 days from now)
        var returnResponse = await librarianClient.PostAsJsonAsync(
            $"/api/reservations/{reservationId}/return",
            new ReturnRequest { Condition = "Good", Notes = "No damage" });

        Assert.Equal(HttpStatusCode.OK, returnResponse.StatusCode);

        var body = await returnResponse.Content.ReadFromJsonAsync<ReturnResponse>();
        Assert.NotNull(body);
        Assert.Equal(reservationId, body.ReservationId);
        Assert.Equal(0, body.LateDays);
        Assert.Equal(0m, body.LateFee);
        Assert.NotEmpty(body.Message);
    }

    // ─── 7. Return_LateReturn_HasLateFee ─────────────────────────────────────

    [Fact]
    public async Task Return_LateReturn_HasLateFee()
    {
        var reservationId = await CreateReservationAsync(Guid.NewGuid());

        var librarianClient = ClientWithToken("Librarian");

        // Checkout
        await librarianClient.PostAsJsonAsync(
            $"/api/reservations/{reservationId}/checkout",
            new CheckoutRequest());

        // Manually set DueDate to 5 days in the past via the DB
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ReservationServiceContext>();
            var reservation = await db.Reservations.FindAsync(reservationId);
            Assert.NotNull(reservation);
            reservation.DueDate = DateTime.UtcNow.AddDays(-5);
            await db.SaveChangesAsync();
        }

        // Return — expect late fee
        var returnResponse = await librarianClient.PostAsJsonAsync(
            $"/api/reservations/{reservationId}/return",
            new ReturnRequest { Condition = "Good" });

        Assert.Equal(HttpStatusCode.OK, returnResponse.StatusCode);

        var body = await returnResponse.Content.ReadFromJsonAsync<ReturnResponse>();
        Assert.NotNull(body);
        Assert.True(body.LateDays > 0,
            $"Expected LateDays > 0 but got {body.LateDays}");
        Assert.True(body.LateFee > 0m,
            $"Expected LateFee > 0 but got {body.LateFee}");
    }

    // ─── 8. GetHistory_Returns200WithPagination ───────────────────────────────

    [Fact]
    public async Task GetHistory_Returns200WithPagination()
    {
        var client = ClientWithToken("Patron");
        var response = await client.GetAsync("/api/reservations/history?page=0&size=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PaginatedResponse<HistoryItemDto>>();
        Assert.NotNull(body);
        Assert.NotNull(body.Content);
        Assert.Equal(10, body.Size);
        Assert.True(body.TotalPages >= 0);
        Assert.Equal(0, body.Page);
    }

    // ─── 9. JoinWaitlist_WhenBookUnavailable_Returns201 ──────────────────────

    [Fact]
    public async Task JoinWaitlist_WhenBookUnavailable_Returns201()
    {
        // No copies available → waitlist should succeed
        _mockState.BookAvailableCopies = 0;
        _mockState.BookTitle = "Out of Stock Book";
        _mockState.BookAuthor = "Some Author";

        var client = ClientWithToken("Patron");
        var bookId = Guid.NewGuid();
        var payload = new JoinWaitlistRequest { BookId = bookId };

        var response = await client.PostAsJsonAsync("/api/reservations/waitlist", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JoinWaitlistResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.WaitlistId);
        Assert.Equal(bookId, body.BookId);
        Assert.Equal("Waiting", body.Status);
        Assert.True(body.Position >= 1);
    }

    // ─── 10. JoinWaitlist_WhenBookAvailable_Returns400_BOOK_AVAILABLE ─────────

    [Fact]
    public async Task JoinWaitlist_WhenBookAvailable_Returns400_BOOK_AVAILABLE()
    {
        // Copies available → should reject waitlist and ask user to reserve instead
        _mockState.BookAvailableCopies = 3;
        _mockState.BookTitle = "Available Book";
        _mockState.BookAuthor = "Author Name";

        var client = ClientWithToken("Patron");
        var payload = new JoinWaitlistRequest { BookId = Guid.NewGuid() };

        var response = await client.PostAsJsonAsync("/api/reservations/waitlist", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var responseText = await response.Content.ReadAsStringAsync();
        Assert.Contains("BOOK_AVAILABLE", responseText, StringComparison.OrdinalIgnoreCase);
    }
}
