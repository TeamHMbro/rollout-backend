using Microsoft.EntityFrameworkCore;
using Rollout.Modules.Users.Entities;

namespace Rollout.Modules.Users.Data;

public sealed class UsersDbContext : DbContext
{
    public UsersDbContext(DbContextOptions<UsersDbContext> options) : base(options)
    {
    }

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("users");

        modelBuilder.Entity<UserProfile>(builder =>
        {
            builder.ToTable("profiles");

            builder.HasKey(x => x.UserId);

            builder.Property(x => x.Username)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.DisplayName)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.City)
                .HasMaxLength(100);

            builder.Property(x => x.Bio)
                .HasMaxLength(500);

            builder.Property(x => x.AvatarUrl)
                .HasMaxLength(500);

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();

            builder.HasIndex(x => x.Username)
                .IsUnique();
        });
    }
}