using Events.Domain.Events;
using Events.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Events.Infrastructure;

public class EventDbContext : DbContext
{
    public DbSet<UserProfile> Users => Set<UserProfile>();
    public DbSet<EventEntity> Events => Set<EventEntity>();
    public DbSet<EventMember> EventMembers => Set<EventMember>();
    public DbSet<LikedPost> LikedPosts => Set<LikedPost>();
    public DbSet<SavedPost> SavedPosts => Set<SavedPost>();

    public EventDbContext(DbContextOptions<EventDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserProfile>(b =>
        {
            b.ToTable("users");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            b.Property(x => x.UserName).HasColumnName("user_name").HasMaxLength(50).IsRequired();
            b.Property(x => x.Avatar).HasColumnName("avatar");
            b.Property(x => x.City).HasColumnName("city");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
            b.HasIndex(x => x.UserName).IsUnique();
        });

        modelBuilder.Entity<EventEntity>(b =>
        {
            b.ToTable("events");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.OwnerId).HasColumnName("owner_id").IsRequired();
            b.Property(x => x.Title).HasColumnName("title").HasMaxLength(150).IsRequired();
            b.Property(x => x.Description).HasColumnName("description");
            b.Property(x => x.Type).HasColumnName("type").HasConversion<string>().IsRequired();
            b.Property(x => x.City).HasColumnName("city").HasMaxLength(100).IsRequired();
            b.Property(x => x.Address).HasColumnName("address").HasMaxLength(255).IsRequired();
            b.Property(x => x.Visibility).HasColumnName("visibility").HasConversion<string>().IsRequired();
            b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired();
            b.Property(x => x.MaxMembers).HasColumnName("max_members");
            b.Property(x => x.MembersCount).HasColumnName("members_count").IsRequired();
            b.Property(x => x.Price).HasColumnName("price");
            b.Property(x => x.Payment).HasColumnName("payment").HasConversion<string>();
            b.Property(x => x.EventStartAt).HasColumnName("event_start_at").IsRequired();
            b.Property(x => x.EventEndAt).HasColumnName("event_end_at");
            b.Property(x => x.PostDate).HasColumnName("post_date").IsRequired();
            b.Property(x => x.IsRecurring).HasColumnName("is_recurring").IsRequired();
            b.Property(x => x.RecurrenceRule).HasColumnName("recurrence_rule").HasMaxLength(255);
            b.Property(x => x.CallLink).HasColumnName("call_link").HasMaxLength(255);
            b.Property(x => x.LikesCount).HasColumnName("likes_count").IsRequired();
            b.Property(x => x.ViewCount).HasColumnName("view_count").IsRequired();
            b.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();

            b.HasIndex(x => new { x.City, x.Status, x.EventStartAt });
        });

        modelBuilder.Entity<EventMember>(b =>
        {
            b.ToTable("event_members");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.EventId).HasColumnName("event_id").IsRequired();
            b.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired();
            b.Property(x => x.Role).HasColumnName("role").HasConversion<string>().IsRequired();
            b.Property(x => x.JoinedAt).HasColumnName("joined_at").IsRequired();

            b.HasIndex(x => new { x.EventId, x.UserId }).IsUnique();
            b.HasIndex(x => new { x.UserId, x.Status });
        });

        modelBuilder.Entity<LikedPost>(b =>
        {
            b.ToTable("liked_posts");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            b.Property(x => x.EventId).HasColumnName("event_id").IsRequired();
            b.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            b.HasIndex(x => new { x.UserId, x.EventId }).IsUnique();
        });

        modelBuilder.Entity<SavedPost>(b =>
        {
            b.ToTable("saved_posts");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            b.Property(x => x.EventId).HasColumnName("event_id").IsRequired();
            b.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            b.HasIndex(x => new { x.UserId, x.EventId }).IsUnique();
        });
    }
}
