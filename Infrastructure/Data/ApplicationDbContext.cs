using Microsoft.EntityFrameworkCore;
using SEM.Domain.Models;

namespace SEM.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public DbSet<User> Users { get; set; }

    public DbSet<Event> Events { get; set; }

    public DbSet<Category> Categories { get; set; }

    public DbSet<EventCategory> EventCategories { get; set; }
    
    public DbSet<EventRole> EventRoles { get; set; }
    
    public DbSet<Roles> Roles { get; set; }
    
    public DbSet<EventPhoto> EventPhotos { get; set; }


    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
        Database.EnsureCreated();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

        modelBuilder.Entity<Category>()
            .HasIndex(c => c.Name)
            .IsUnique();

        modelBuilder.Entity<EventCategory>()
            .HasKey(ec => new { ec.EventId, ec.CategoryId });

        modelBuilder.Entity<EventCategory>()
            .HasOne(ec => ec.Event)
            .WithMany(e => e.EventCategories)
            .HasForeignKey(ec => ec.EventId);

        modelBuilder.Entity<EventCategory>()
            .HasOne(ec => ec.Category)
            .WithMany(c => c.EventCategories)
            .HasForeignKey(ec => ec.CategoryId);
        
        modelBuilder.Entity<EventRole>()
            .HasKey(ec => new { ec.EventId, ec.UserId, ec.RoleId });
        
        modelBuilder.Entity<EventRole>()
            .HasOne(eur => eur.User)
            .WithMany(u => u.EventRole)
            .HasForeignKey(eur => eur.UserId);

        modelBuilder.Entity<EventRole>()
            .HasOne(eur => eur.Event)
            .WithMany(e => e.EventRoles)
            .HasForeignKey(eur => eur.EventId);

        modelBuilder.Entity<EventRole>()
            .HasOne(eur => eur.Role)
            .WithMany(r => r.EventRoles)
            .HasForeignKey(eur => eur.RoleId);
    }
}