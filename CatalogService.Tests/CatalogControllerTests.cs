using System.Net;
using System.Net.Http.Json;
using CatalogService.Data;
using CatalogService.Models.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CatalogService.Tests;

/// <summary>
/// Custom WebApplicationFactory for CatalogService integration tests.
/// Uses InMemory database and relies on the service's own CatalogSeedData seeder
/// which runs on app startup in Development mode.
/// </summary>
public class CatalogServiceWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"CatalogTestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Development mode avoids the connection string requirement and
        // activates the InMemory + CatalogSeedData path in Program.cs.
        builder.UseEnvironment("Development");

        // ConfigureTestServices runs AFTER the app's own services are registered.
        builder.ConfigureTestServices(services =>
        {
            // Remove the default InMemory "CatalogDb" and replace with a unique name
            // so each test class gets a fresh, isolated DB.
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<CatalogServiceContext>));
            if (dbDescriptor != null) services.Remove(dbDescriptor);

            services.AddDbContext<CatalogServiceContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });
    }
}

public class CatalogControllerTests : IClassFixture<CatalogServiceWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CatalogControllerTests(CatalogServiceWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ─── GET /api/catalog/books (default params) ──────────────────────────────

    [Fact]
    public async Task GetBooks_WithDefaults_Returns200WithPaginatedList()
    {
        var response = await _client.GetAsync("/api/catalog/books");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PaginatedResponse<BookSummaryDto>>();
        Assert.NotNull(body);
        Assert.NotNull(body.Content);
        Assert.True(body.Content.Count > 0);
        Assert.True(body.TotalElements >= body.Content.Count);
        Assert.True(body.Size > 0);
        Assert.True(body.TotalPages >= 1);
    }

    // ─── GET /api/catalog/books?query=Clean ──────────────────────────────────

    [Fact]
    public async Task GetBooks_WithTitleQuery_FiltersResults()
    {
        var response = await _client.GetAsync("/api/catalog/books?query=Clean");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PaginatedResponse<BookSummaryDto>>();
        Assert.NotNull(body);
        Assert.True(body.Content.Count > 0, "Expected at least one book matching 'Clean'");

        // Every returned book must contain 'Clean' in title or author
        foreach (var book in body.Content)
        {
            var inTitle = book.Title.Contains("Clean", StringComparison.OrdinalIgnoreCase);
            var inAuthor = book.Author.Contains("Clean", StringComparison.OrdinalIgnoreCase);
            Assert.True(inTitle || inAuthor,
                $"'{book.Title}' by '{book.Author}' does not contain 'Clean'");
        }
    }

    // ─── GET /api/catalog/books?genre=Technology ─────────────────────────────

    [Fact]
    public async Task GetBooks_WithGenreFilter_FiltersResults()
    {
        var response = await _client.GetAsync("/api/catalog/books?genre=Technology");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PaginatedResponse<BookSummaryDto>>();
        Assert.NotNull(body);
        Assert.True(body.Content.Count > 0, "Expected Technology books in seed data");

        foreach (var book in body.Content)
        {
            Assert.Equal("Technology", book.Genre, ignoreCase: true);
        }
    }

    // ─── GET /api/catalog/books?availableOnly=true ───────────────────────────

    [Fact]
    public async Task GetBooks_WithAvailableOnly_FiltersResults()
    {
        var response = await _client.GetAsync("/api/catalog/books?availableOnly=true");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PaginatedResponse<BookSummaryDto>>();
        Assert.NotNull(body);
        Assert.True(body.Content.Count > 0, "Expected at least one available book in seed data");

        foreach (var book in body.Content)
        {
            Assert.True(book.AvailableCopies > 0,
                $"'{book.Title}' has {book.AvailableCopies} copies but availableOnly=true was requested");
        }
    }

    // ─── GET /api/catalog/books?sortBy=author&sortOrder=asc ──────────────────

    [Fact]
    public async Task GetBooks_WithSortByAuthor_ReturnsSorted()
    {
        var response = await _client.GetAsync("/api/catalog/books?sortBy=author&sortOrder=asc&size=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PaginatedResponse<BookSummaryDto>>();
        Assert.NotNull(body);
        Assert.True(body.Content.Count > 1, "Need at least 2 books to verify sorting");

        for (int i = 1; i < body.Content.Count; i++)
        {
            var prev = body.Content[i - 1].Author;
            var curr = body.Content[i].Author;
            Assert.True(
                string.Compare(prev, curr, StringComparison.OrdinalIgnoreCase) <= 0,
                $"Sort broken: '{prev}' should come before '{curr}'"
            );
        }
    }

    // ─── GET /api/catalog/books/{id} (valid) ─────────────────────────────────

    [Fact]
    public async Task GetBookById_WithValidId_Returns200WithDetails()
    {
        // First, list books to obtain a real ID from the seeded data
        var listResponse = await _client.GetAsync("/api/catalog/books");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var listBody = await listResponse.Content.ReadFromJsonAsync<PaginatedResponse<BookSummaryDto>>();
        Assert.NotNull(listBody);
        Assert.NotEmpty(listBody.Content);

        var firstBook = listBody.Content[0];

        // Now fetch by ID
        var response = await _client.GetAsync($"/api/catalog/books/{firstBook.BookId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<BookDetailDto>();
        Assert.NotNull(body);
        Assert.Equal(firstBook.BookId, body.BookId);
        Assert.Equal(firstBook.Title, body.Title);
        Assert.Equal(firstBook.Author, body.Author);
        Assert.True(body.TotalCopies >= 0);
        Assert.NotEmpty(body.Status);
    }

    // ─── GET /api/catalog/books/{id} (non-existent) ──────────────────────────

    [Fact]
    public async Task GetBookById_WithInvalidId_Returns404()
    {
        var randomId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/catalog/books/{randomId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("NOT_FOUND", body.Error);
    }

    // ─── GET /api/catalog/books?query=xyznotexist ────────────────────────────

    [Fact]
    public async Task GetBooks_EmptyQuery_ReturnsEmptyContent()
    {
        var response = await _client.GetAsync("/api/catalog/books?query=xyznotexistinthisdb");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PaginatedResponse<BookSummaryDto>>();
        Assert.NotNull(body);
        Assert.Empty(body.Content);
        Assert.Equal(0, body.TotalElements);
    }
}
