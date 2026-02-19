using EchoHub.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EchoHub.Server.Data;

public class EchoHubDbContext : DbContext
{
    public EchoHubDbContext(DbContextOptions<EchoHubDbContext> options) : base(options) { }
    public DbSet<User> Users => Set<User>();
    public DbSet<Channel> Channels => Set<Channel>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var dbPath = Path.Combine(AppContext.BaseDirectory, "echohub.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.DisplayName).HasMaxLength(100);
            entity.Property(u => u.Bio).HasMaxLength(500);
            entity.Property(u => u.NicknameColor).HasMaxLength(7);
            entity.Property(u => u.AvatarAscii).HasMaxLength(10000);
            entity.Property(u => u.StatusMessage).HasMaxLength(100);
        });

        modelBuilder.Entity<Channel>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => c.Name).IsUnique();
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            entity.Property(c => c.Topic).HasMaxLength(500);

            entity.HasMany(c => c.Messages)
                  .WithOne(m => m.Channel)
                  .HasForeignKey(m => m.ChannelId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.HasIndex(m => m.SentAt);
            entity.Property(m => m.Content).IsRequired().HasMaxLength(2000);
            entity.Property(m => m.SenderUsername).IsRequired().HasMaxLength(50);
            entity.Property(m => m.AttachmentUrl).HasMaxLength(500);
            entity.Property(m => m.AttachmentFileName).HasMaxLength(255);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => r.TokenHash);
            entity.HasIndex(r => r.UserId);
            entity.Property(r => r.TokenHash).IsRequired().HasMaxLength(128);

            entity.HasOne(r => r.User)
                  .WithMany()
                  .HasForeignKey(r => r.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // SQLite does not support DateTimeOffset in ORDER BY clauses.
        // Convert all DateTimeOffset properties to Unix milliseconds (long) for storage.
        var dateTimeOffsetConverter = new ValueConverter<DateTimeOffset, long>(
            v => v.ToUnixTimeMilliseconds(),
            v => DateTimeOffset.FromUnixTimeMilliseconds(v));

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTimeOffset) || property.ClrType == typeof(DateTimeOffset?))
                {
                    property.SetValueConverter(dateTimeOffsetConverter);
                }
            }
        }
    }
}
