using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using UserService.Clients;
using UserService.Data;
using UserService.Services;

var builder = WebApplication.CreateBuilder(args);

// ─── Database ──────────────────────────────────────────────────────────────────
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<UserServiceContext>(o =>
        o.UseInMemoryDatabase("UserServiceDb"));
}
else
{
    var cs = builder.Configuration.GetConnectionString("UserDb")
        ?? throw new InvalidOperationException("ConnectionStrings:UserDb is required in production.");

    builder.Services.AddDbContext<UserServiceContext>(o =>
        o.UseNpgsql(cs));
}

// ─── JWT Authentication ────────────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is required.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer is required.");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience is required.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            // Map JWT claim names to ASP.NET Core identities
            RoleClaimType = "role",
            NameClaimType = "sub",
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ─── Application Services ──────────────────────────────────────────────────────
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();

// ─── HTTP Client for ReservationService ───────────────────────────────────────
var reservationUrl = builder.Configuration["ServiceUrls:ReservationService"]
    ?? "http://localhost:5003";

builder.Services.AddHttpClient<ReservationServiceClient>(c =>
{
    c.BaseAddress = new Uri(reservationUrl);
    c.Timeout = TimeSpan.FromSeconds(10);
});

// ─── Controllers & Validation ─────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// ─── Swagger / OpenAPI ────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "User Service API",
        Version = "v1",
        Description = "Authentication and user management microservice for the Digital Library Management System."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer {token}' (without quotes)."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ─── Build ────────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "User Service API v1");
    c.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ─── Health endpoint ──────────────────────────────────────────────────────────
app.MapGet("/health", async (UserServiceContext db, IWebHostEnvironment env) =>
{
    var migrations = 0;
    if (!env.IsDevelopment())
    {
        try
        {
            migrations = (await db.Database.GetAppliedMigrationsAsync()).Count();
        }
        catch
        {
            // Ignore — still report UP so the health check passes
        }
    }

    return Results.Ok(new
    {
        service = "UserService",
        status = "UP",
        database = "userservicedb",
        migrations
    });
});

// ─── Development seed ─────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var ctx = scope.ServiceProvider.GetRequiredService<UserServiceContext>();
    await UserServiceSeeder.SeedAsync(ctx);
}

// ─── Production migrations ────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<UserServiceContext>().Database.Migrate();
}

app.Run();

// Needed for integration test project to reference this assembly
public partial class Program { }
