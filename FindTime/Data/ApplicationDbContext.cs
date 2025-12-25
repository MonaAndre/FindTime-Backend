using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using FindTime.Models;

namespace FindTime.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets
    public DbSet<Group> Groups { get; set; }
    public DbSet<GroupUser> GroupUsers { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<EventParticipant> EventParticipants { get; set; }
    public DbSet<EventNote> EventNotes { get; set; }
    public DbSet<EventReminder> EventReminders { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<UserGroupSettings> UserGroupSettings { get; set; }
    public DbSet<UserMemberSettings> UserMemberSettings { get; set; }

    

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Group
        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.GroupId);
            entity.Property(e => e.GroupId).UseIdentityColumn();

            entity.HasOne(g => g.Admin)
                .WithMany()
                .HasForeignKey(g => g.AdminId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.GroupName);
        });

        // Configure GroupUser
        modelBuilder.Entity<GroupUser>(entity =>
        {
            entity.HasKey(e => e.GroupUserId);
            entity.Property(e => e.GroupUserId).UseIdentityColumn();

            entity.HasOne(gu => gu.User)
                .WithMany(u => u.GroupUsers)
                .HasForeignKey(gu => gu.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(gu => gu.Group)
                .WithMany(g => g.GroupUsers)
                .HasForeignKey(gu => gu.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            // Composite unique index to prevent duplicate user-group combinations
            entity.HasIndex(e => new { e.UserId, e.GroupId }).IsUnique();
        });

        // Configure Category
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId);
            entity.Property(e => e.CategoryId).UseIdentityColumn();

            entity.HasOne(c => c.Group)
                .WithMany(g => g.Categories)
                .HasForeignKey(c => c.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.CreatedBy)
                .WithMany(u => u.Categories)
                .HasForeignKey(c => c.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Event
        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.EventId);
            entity.Property(e => e.EventId).UseIdentityColumn();

            entity.HasOne(e => e.Group)
                .WithMany(g => g.Events)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Events)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Creator)
                .WithMany(u => u.CreatedEvents)
                .HasForeignKey(e => e.CreatorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.StartTime);
            entity.HasIndex(e => new { e.GroupId, e.StartTime });
        });

        // Configure EventParticipant
        modelBuilder.Entity<EventParticipant>(entity =>
        {
            entity.HasKey(e => e.EventParticipantId);
            entity.Property(e => e.EventParticipantId).UseIdentityColumn();

            entity.HasOne(ep => ep.Event)
                .WithMany(e => e.EventParticipants)
                .HasForeignKey(ep => ep.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ep => ep.User)
                .WithMany(u => u.EventParticipants)
                .HasForeignKey(ep => ep.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Composite unique index to prevent duplicate event-user combinations
            entity.HasIndex(e => new { e.EventId, e.UserId }).IsUnique();
        });

        // Configure EventNote
        modelBuilder.Entity<EventNote>(entity =>
        {
            entity.HasKey(e => e.EventNoteId);
            entity.Property(e => e.EventNoteId).UseIdentityColumn();

            entity.HasOne(en => en.Event)
                .WithMany(e => e.EventNotes)
                .HasForeignKey(en => en.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(en => en.User)
                .WithMany(u => u.EventNotes)
                .HasForeignKey(en => en.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.CreatedAt);
        });

        // Configure EventReminder
        modelBuilder.Entity<EventReminder>(entity =>
        {
            entity.HasKey(e => e.EventReminderId);
            entity.Property(e => e.EventReminderId).UseIdentityColumn();

            entity.HasOne(er => er.Event)
                .WithMany(e => e.EventReminders)
                .HasForeignKey(er => er.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(er => er.User)
                .WithMany(u => u.EventReminders)
                .HasForeignKey(er => er.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Composite unique index to prevent duplicate event-user-time combinations
            entity.HasIndex(e => new { e.EventId, e.UserId, e.MinutesBeforeEvent }).IsUnique();
        });

        // Configure Notification
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId);
            entity.Property(e => e.NotificationId).UseIdentityColumn();

            entity.HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(n => n.Event)
                .WithMany(e => e.Notifications)
                .HasForeignKey(n => n.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.IsRead, e.CreatedAt });
        });

        // Configure UserGroupSettings
        modelBuilder.Entity<UserGroupSettings>(entity =>
        {
            entity.HasKey(e => e.UserGroupSettingsId);
            entity.Property(e => e.UserGroupSettingsId).UseIdentityColumn();

            entity.HasOne(ugs => ugs.User)
                .WithMany(u => u.UserGroupSettings)
                .HasForeignKey(ugs => ugs.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ugs => ugs.Group)
                .WithMany(g => g.UserGroupSettings)
                .HasForeignKey(ugs => ugs.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            // Composite unique index to ensure one setting per user-group combination
            entity.HasIndex(e => new { e.UserId, e.GroupId }).IsUnique();
        });

        // Configure UserMemberSettings
        modelBuilder.Entity<UserMemberSettings>(entity =>
        {
            entity.HasKey(e => e.UserMemberSettingsId);
            entity.Property(e => e.UserMemberSettingsId).UseIdentityColumn();

            entity.HasOne(ums => ums.User)
                .WithMany(u => u.UserMemberSettingsAsUser)
                .HasForeignKey(ums => ums.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ums => ums.TargetUser)
                .WithMany(u => u.UserMemberSettingsAsTarget)
                .HasForeignKey(ums => ums.TargetUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ums => ums.Group)
                .WithMany(g => g.UserMemberSettings)
                .HasForeignKey(ums => ums.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            // Composite unique index to ensure one nickname per user-target-group combination
            entity.HasIndex(e => new { e.UserId, e.TargetUserId, e.GroupId }).IsUnique();

            // Prevent users from assigning nicknames to themselves
            entity.HasCheckConstraint("CK_UserMemberSettings_DifferentUsers", 
                "\"UserId\" != \"TargetUserId\"");
        });

        // Configure ApplicationUser additional indexes
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.IsDeleted);
        });
    }
}