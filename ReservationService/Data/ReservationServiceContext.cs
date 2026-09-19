using Microsoft.EntityFrameworkCore;
using ReservationService.Models.Entities;
using ReservationService.Models.Enums;

namespace ReservationService.Data;

public class ReservationServiceContext : DbContext
{
    public ReservationServiceContext(DbContextOptions<ReservationServiceContext> options) : base(options)
    {
    }

    public DbSet<Reservation> Reservations => Set<Reservation>();

    public DbSet<WaitlistEntry> WaitlistEntries => Set<WaitlistEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(r => r.ReservationId);

            entity.HasIndex(r => r.UserId);
            entity.HasIndex(r => r.BookId);
            entity.HasIndex(r => r.Status);
            entity.HasIndex(r => new { r.UserId, r.BookId });
            entity.HasIndex(r => new { r.BookId, r.Status });

            entity.Property(r => r.Status)
                  .HasConversion<string>()
                  .HasMaxLength(50);

            entity.Property(r => r.Condition)
                  .HasConversion<string?>()
                  .HasMaxLength(50);

            entity.Property(r => r.BookTitle)
                  .IsRequired()
                  .HasMaxLength(255);

            entity.Property(r => r.BookAuthor)
                  .IsRequired()
                  .HasMaxLength(255);

            entity.Property(r => r.LateFee)
                  .HasColumnType("decimal(18,2)");

            entity.Property(r => r.RenewalCount)
                  .HasDefaultValue(0);
        });

        modelBuilder.Entity<WaitlistEntry>(entity =>
        {
            entity.HasKey(w => w.WaitlistId);

            entity.HasIndex(w => w.UserId);
            entity.HasIndex(w => w.BookId);
            entity.HasIndex(w => w.Status);
            entity.HasIndex(w => new { w.UserId, w.BookId });
            entity.HasIndex(w => new { w.BookId, w.Status });

            entity.Property(w => w.Status)
                  .HasConversion<string>()
                  .HasMaxLength(50);

            entity.Property(w => w.BookTitle)
                  .IsRequired()
                  .HasMaxLength(255);

            entity.Property(w => w.BookAuthor)
                  .IsRequired()
                  .HasMaxLength(255);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<Reservation>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;

                if (entry.Entity.ReservedAt == default)
                    entry.Entity.ReservedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        foreach (var entry in ChangeTracker.Entries<WaitlistEntry>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;

                if (entry.Entity.JoinedAt == default)
                    entry.Entity.JoinedAt = now;
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

        foreach (var entry in ChangeTracker.Entries<Reservation>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;

                if (entry.Entity.ReservedAt == default)
                    entry.Entity.ReservedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        foreach (var entry in ChangeTracker.Entries<WaitlistEntry>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;

                if (entry.Entity.JoinedAt == default)
                    entry.Entity.JoinedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        return base.SaveChanges();
    }
}
