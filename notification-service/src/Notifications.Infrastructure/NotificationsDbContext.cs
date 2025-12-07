using Microsoft.EntityFrameworkCore;
using Notifications.Domain.Notifications;

namespace Notifications.Infrastructure;

public class NotificationsDbContext : DbContext
{
    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : base(options)
    {
    }

    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("public");

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(x => x.Type).HasColumnName("type").IsRequired();
            entity.Property(x => x.Payload).HasColumnName("payload").IsRequired().HasColumnType("jsonb");
            entity.Property(x => x.IsRead).HasColumnName("is_read").IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(x => x.ReadAt).HasColumnName("read_at");
            entity.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt });
        });
    }
}
