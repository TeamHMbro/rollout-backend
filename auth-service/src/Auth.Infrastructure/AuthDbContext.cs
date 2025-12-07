using Auth.Domain.Tokens;
using Auth.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure;

public class AuthDbContext : DbContext
{
    public DbSet<AuthUser> Users => Set<AuthUser>();
    public DbSet<AuthProvider> Providers => Set<AuthProvider>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");

        modelBuilder.Entity<AuthUser>(b =>
        {
            b.ToTable("auth_users");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();
            b.Property(x => x.Email).HasColumnName("email");
            b.Property(x => x.Phone).HasColumnName("phone");
            b.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired();
            b.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
            b.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
            b.HasIndex(x => x.Email).IsUnique();
            b.HasIndex(x => x.Phone).IsUnique();
        });

        modelBuilder.Entity<AuthProvider>(b =>
        {
            b.ToTable("auth_providers");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            b.Property(x => x.Provider).HasColumnName("provider").HasMaxLength(50).IsRequired();
            b.Property(x => x.ProviderId).HasColumnName("provider_id").HasMaxLength(255).IsRequired();
            b.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            b.HasIndex(x => new { x.Provider, x.ProviderId }).IsUnique();
        });

        modelBuilder.Entity<RefreshToken>(b =>
        {
            b.ToTable("auth_refresh_tokens");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            b.Property(x => x.Token).HasColumnName("token").HasMaxLength(255).IsRequired();
            b.Property(x => x.ExpiresAt).HasColumnName("expires_at").IsRequired();
            b.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            b.Property(x => x.RevokedAt).HasColumnName("revoked_at");
            b.HasIndex(x => x.Token).IsUnique();
        });
    }
}
