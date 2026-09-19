using Microsoft.EntityFrameworkCore;
using CatalogService.Models.Entities;

namespace CatalogService.Data;

public class CatalogServiceContext : DbContext
{
    public CatalogServiceContext(DbContextOptions<CatalogServiceContext> options) : base(options)
    {
    }

    public DbSet<Book> Books => Set<Book>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(b => b.BookId);

            entity.HasIndex(b => b.Isbn)
                  .IsUnique();

            entity.Property(b => b.Isbn)
                  .IsRequired()
                  .HasMaxLength(20);

            entity.Property(b => b.Title)
                  .IsRequired()
                  .HasMaxLength(255);

            entity.Property(b => b.Author)
                  .IsRequired()
                  .HasMaxLength(255);

            entity.Property(b => b.Genre)
                  .HasMaxLength(100);

            entity.Property(b => b.Publisher)
                  .HasMaxLength(255);

            entity.Property(b => b.Language)
                  .HasMaxLength(50);

            entity.Property(b => b.TotalCopies)
                  .HasDefaultValue(0);

            entity.Property(b => b.AvailableCopies)
                  .HasDefaultValue(0);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<Book>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<Book>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        return base.SaveChanges();
    }
}
