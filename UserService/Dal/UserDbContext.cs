using Dal.UserProfiles.Models;
using Microsoft.EntityFrameworkCore;

namespace Dal;

public class UserDbContext : DbContext
{
    public DbSet<UserProfile> UserProfiles { get; set; }

    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserProfile>()
            .HasKey(p => p.Id);
    }
} 