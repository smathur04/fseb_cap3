using UserService.Models.Entities;
using UserService.Models.Enums;

namespace UserService.Data;

public static class UserServiceSeeder
{
    private const string TestPassword = "Test123!@#";
    private const int BcryptWorkFactor = 12;

    public static async Task SeedAsync(UserServiceContext context)
    {
        // Only seed if the table is empty to avoid duplicates on restart
        if (context.Users.Any())
            return;

        var now = DateTime.UtcNow;
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(TestPassword, BcryptWorkFactor);

        var users = new List<User>
        {
            new User
            {
                UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Email = "patron@library.test",
                PasswordHash = passwordHash,
                FirstName = "Jane",
                LastName = "Patron",
                PhoneNumber = "555-0101",
                Role = Role.Patron,
                MembershipStatus = MembershipStatus.Active,
                MemberSince = now,
                CreatedAt = now,
                UpdatedAt = now
            },
            new User
            {
                UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Email = "librarian@library.test",
                PasswordHash = passwordHash,
                FirstName = "John",
                LastName = "Librarian",
                PhoneNumber = "555-0102",
                Role = Role.Librarian,
                MembershipStatus = MembershipStatus.Active,
                MemberSince = now,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        context.Users.AddRange(users);
        await context.SaveChangesAsync();
    }
}
