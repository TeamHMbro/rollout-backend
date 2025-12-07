using Chat.Domain.EventMessages;
using Microsoft.EntityFrameworkCore;

namespace Chat.Infrastructure;

public sealed class ChatDbContext : DbContext
{
    public ChatDbContext(DbContextOptions<ChatDbContext> options)
        : base(options)
    {
    }

    public DbSet<EventMessage> EventMessages => Set<EventMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");

        modelBuilder.Entity<EventMessage>(b =>
        {
            b.ToTable("event_messages");
            b.HasKey(x => x.Id);

            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.EventId).HasColumnName("event_id").IsRequired();
            b.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            b.Property(x => x.Content).HasColumnName("content").IsRequired();
            b.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            b.Property(x => x.EditedAt).HasColumnName("edited_at");
            b.Property(x => x.IsDeleted).HasColumnName("is_deleted").IsRequired();

            b.HasIndex(x => new { x.EventId, x.CreatedAt });
        });
    }
}
