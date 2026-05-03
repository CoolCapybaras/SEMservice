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
    
    public DbSet<EventPhoto> EventPhotos { get; set; }
    
    public DbSet<Notification> Notifications { get; set; }
    
    public DbSet<EventPost> EventPosts { get; set; }

    public DbSet<Invites> Invites { get; set; }

    public DbSet<RefreshToken> RefreshTokens { get; set; }
    
    public DbSet<BoardColumn> BoardColumn { get; set; }
    
    public DbSet<BoardTask> BoardTasks { get; set; }
    
    public DbSet<BoardTaskComment> BoardTaskComments { get; set; }
    
    public DbSet<BoardTaskHistory> BoardTaskHistories { get; set; }
    
    public DbSet<EventAttachment> EventAttachments { get; set; }
    
    public DbSet<EventNote> EventNotes { get; set; }


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
            .HasKey(er => new { er.EventId, er.UserId });
        
        modelBuilder.Entity<EventRole>()
            .HasOne(eur => eur.User)
            .WithMany(u => u.EventRole)
            .HasForeignKey(eur => eur.UserId);

        modelBuilder.Entity<EventRole>()
            .HasOne(eur => eur.Event)
            .WithMany(e => e.EventRoles)
            .HasForeignKey(eur => eur.EventId);
        
        modelBuilder.Entity<User>()
            .Property(u => u.Theme)
            .HasConversion<int>();

        modelBuilder.Entity<User>()
            .Property(u => u.NotificationChannel)
            .HasConversion<int>();

        modelBuilder.Entity<BoardTask>()
            .Property(t => t.Priority)
            .HasConversion<int>();

        modelBuilder.Entity<BoardTask>()
            .HasOne(t => t.AssignedUser)
            .WithMany()
            .HasForeignKey(t => t.AssignedUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<BoardTaskComment>()
            .HasOne(c => c.Task)
            .WithMany(t => t.Comments)
            .HasForeignKey(c => c.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BoardTaskComment>()
            .HasOne(c => c.Author)
            .WithMany()
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BoardTaskHistory>()
            .HasOne(h => h.Task)
            .WithMany()
            .HasForeignKey(h => h.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EventAttachment>()
            .Property(a => a.Kind)
            .HasConversion<int>();

        modelBuilder.Entity<EventAttachment>()
            .HasOne(a => a.Event)
            .WithMany(e => e.Attachments)
            .HasForeignKey(a => a.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EventAttachment>()
            .HasOne(a => a.Author)
            .WithMany()
            .HasForeignKey(a => a.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EventNote>()
            .HasOne(n => n.Event)
            .WithMany(e => e.Notes)
            .HasForeignKey(n => n.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EventNote>()
            .HasOne(n => n.Author)
            .WithMany()
            .HasForeignKey(n => n.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
