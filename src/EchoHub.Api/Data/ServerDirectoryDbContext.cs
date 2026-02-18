using EchoHub.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace EchoHub.Api.Data;

public class ServerDirectoryDbContext(DbContextOptions<ServerDirectoryDbContext> options) : DbContext(options)
{
    public DbSet<ServerInfo> Servers => Set<ServerInfo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServerInfo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Url).IsUnique();
        });
    }
}
