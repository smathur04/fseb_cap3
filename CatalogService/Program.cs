using Microsoft.EntityFrameworkCore;
using CatalogService.Data;
using CatalogService.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Database
// ---------------------------------------------------------------------------
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<CatalogServiceContext>(options =>
        options.UseInMemoryDatabase("CatalogDb"));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("CatalogDb")
        ?? throw new InvalidOperationException("Connection string 'CatalogDb' not found.");

    builder.Services.AddDbContext<CatalogServiceContext>(options =>
        options.UseNpgsql(connectionString));
}

// ---------------------------------------------------------------------------
// Application Services
// ---------------------------------------------------------------------------
builder.Services.AddScoped<IBookService, BookService>();

// ---------------------------------------------------------------------------
// ASP.NET Core
// ---------------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "CatalogService API",
        Version = "v1",
        Description = "Digital Library Management System – Catalog microservice. Provides public endpoints to browse and search books."
    });
});

// ---------------------------------------------------------------------------
// CORS (permissive for development; tighten in production via config)
// ---------------------------------------------------------------------------
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

// ---------------------------------------------------------------------------
// Middleware Pipeline
// ---------------------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CatalogService v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors();
app.MapControllers();

// ---------------------------------------------------------------------------
// Health endpoint
// ---------------------------------------------------------------------------
app.MapGet("/health", async (CatalogServiceContext db) =>
{
    int migrations = 0;

    try
    {
        if (!app.Environment.IsDevelopment())
        {
            var applied = await db.Database.GetAppliedMigrationsAsync();
            migrations = applied.Count();
        }
    }
    catch
    {
        // In-memory or migration query failure — leave migrations at 0
    }

    return Results.Ok(new
    {
        service = "CatalogService",
        status = "UP",
        database = "catalogservicedb",
        migrations
    });
});

// ---------------------------------------------------------------------------
// Database initialisation
// ---------------------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogServiceContext>();

    if (app.Environment.IsDevelopment())
    {
        // EnsureCreated is fine for in-memory; then seed
        await db.Database.EnsureCreatedAsync();
        await CatalogSeedData.SeedAsync(db);
    }
    else
    {
        // Apply migrations on startup in production
        await db.Database.MigrateAsync();
    }
}

app.Run();

// Needed by integration-test projects
public partial class Program { }
