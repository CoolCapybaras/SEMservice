using Microsoft.EntityFrameworkCore;
using SEM.Domain.Models;

namespace SEM.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public DbSet<User> Users { get; set; }

    public DbSet<Event> Events { get; set; }
    
    public DbSet<EventChatMessage> EventChatMessages { get; set; }
    public DbSet<EventChatAttachment> EventChatAttachments { get; set; }

    public DbSet<Category> Categories { get; set; }

    public DbSet<EventCategory> EventCategories { get; set; }

    public DbSet<EventSelectedType> EventSelectedTypes { get; set; }
    
    public DbSet<EventRole> EventRoles { get; set; }
    
    public DbSet<Roles> Roles { get; set; }
    
    public DbSet<EventPhoto> EventPhotos { get; set; }
    
    public DbSet<Notification> Notifications { get; set; }
    
    public DbSet<EventPost> EventPosts { get; set; }

    public DbSet<Invites> Invites { get; set; }

    public DbSet<RefreshToken> RefreshTokens { get; set; }
    
    public DbSet<BoardColumn> BoardColumn { get; set; }
    
    public DbSet<BoardTask> BoardTasks { get; set; }


    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
        
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

        modelBuilder.Entity<EventSelectedType>()
            .HasKey(x => new { x.EventId, x.TypeKind });

        modelBuilder.Entity<EventSelectedType>()
            .HasOne(x => x.Event)
            .WithMany(e => e.SelectedTypes)
            .HasForeignKey(x => x.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Event>()
            .Property(e => e.VenueFormat)
            .HasConversion<int>();

        modelBuilder.Entity<Event>()
            .Property(e => e.LifecycleState)
            .HasConversion<int>();

        modelBuilder.Entity<EventSelectedType>()
            .Property(x => x.TypeKind)
            .HasConversion<int>();

        modelBuilder.Entity<EventRole>()
            .Property(er => er.ParticipantRole)
            .HasConversion<int>();
        
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