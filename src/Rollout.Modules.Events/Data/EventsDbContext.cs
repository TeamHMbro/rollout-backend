using Microsoft.EntityFrameworkCore;
using Rollout.Modules.Events.Entities;

namespace Rollout.Modules.Events.Data;

public sealed class EventsDbContext : DbContext
{
    public EventsDbContext(DbContextOptions<EventsDbContext> options) : base(options)
    {
    }

    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventMember> EventMembers => Set<EventMember>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("events");

        modelBuilder.Entity<Event>(builder =>
        {
            builder.ToTable("events");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.Description)
                .HasMaxLength(2000)
                .IsRequired();

            builder.Property(x => x.City)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.PlaceName)
                .HasMaxLength(200);

            builder.Property(x => x.Address)
                .HasMaxLength(300);

            builder.Property(x => x.Category)
                .HasMaxLength(100);

            builder.Property(x => x.MaxMembers)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();

            builder.HasIndex(x => x.CreatorUserId);
            builder.HasIndex(x => new { x.Status, x.StartAtUtc });
            builder.HasIndex(x => x.EndAtUtc);
        });

        modelBuilder.Entity<EventMember>(builder =>
        {
            builder.ToTable("event_members");

            builder.HasKey(x => new { x.EventId, x.UserId });

            builder.Property(x => x.JoinedAtUtc)
                .IsRequired();

            builder.HasIndex(x => x.UserId);

            builder.HasOne(x => x.Event)
                .WithMany(x => x.Members)
                .HasForeignKey(x => x.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}