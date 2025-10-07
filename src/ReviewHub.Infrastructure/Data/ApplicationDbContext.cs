using Microsoft.EntityFrameworkCore;
using ReviewHub.Core.Entities;

namespace ReviewHub.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<PlatformConnection> PlatformConnections => Set<PlatformConnection>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<SmsMessage> SmsMessages => Set<SmsMessage>();
    public DbSet<Competitor> Competitors => Set<Competitor>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<TeamInvitation> TeamInvitations => Set<TeamInvitation>();
    public DbSet<BusinessUser> BusinessUsers => Set<BusinessUser>();
    public DbSet<AISettings> AISettings => Set<AISettings>();

    // Multi-location entities
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<LocationGroup> LocationGroups => Set<LocationGroup>();
    public DbSet<UserLocationAccess> UserLocationAccesses => Set<UserLocationAccess>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Auth0Id).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(255);
        });

        // Business
        modelBuilder.Entity<Business>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.Name });
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Businesses)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // PlatformConnection
        modelBuilder.Entity<PlatformConnection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.BusinessId, e.Platform }).IsUnique();

            entity.HasOne(e => e.Business)
                .WithMany(b => b.PlatformConnections)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Review
        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.BusinessId, e.Platform, e.PlatformReviewId }).IsUnique();
            entity.HasIndex(e => e.ReviewDate);
            entity.HasIndex(e => e.Rating);
            entity.HasIndex(e => e.Sentiment);

            entity.HasOne(e => e.Business)
                .WithMany(b => b.Reviews)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // SmsMessage
        modelBuilder.Entity<SmsMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TwilioMessageSid);
            entity.HasIndex(e => e.SentAt);

            entity.HasOne(e => e.Business)
                .WithMany(b => b.SmsMessages)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Competitor
        modelBuilder.Entity<Competitor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.BusinessId, e.Platform, e.PlatformBusinessId }).IsUnique();

            entity.HasOne(e => e.Business)
                .WithMany(b => b.Competitors)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Customer
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.BusinessId, e.PhoneNumber }).IsUnique();

            entity.HasOne(e => e.Business)
                .WithMany(b => b.Customers)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Campaign
        modelBuilder.Entity<Campaign>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ScheduledFor);

            entity.HasOne(e => e.Business)
                .WithMany(b => b.Campaigns)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Notification
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.IsRead });
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // NotificationPreference
        modelBuilder.Entity<NotificationPreference>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TeamInvitation
        modelBuilder.Entity<TeamInvitation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => new { e.BusinessId, e.Status });

            entity.HasOne(e => e.Business)
                .WithMany()
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.InvitedBy)
                .WithMany()
                .HasForeignKey(e => e.InvitedByUserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // BusinessUser
        modelBuilder.Entity<BusinessUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.BusinessId, e.UserId }).IsUnique();

            entity.HasOne(e => e.Business)
                .WithMany()
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // AISettings
        modelBuilder.Entity<AISettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ===== Multi-Location Entities =====

        // Organization
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
        });

        // LocationGroup (hierarchical)
        modelBuilder.Entity<LocationGroup>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.ParentGroupId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);

            entity.HasOne(e => e.Organization)
                .WithMany(o => o.LocationGroups)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ParentGroup)
                .WithMany(lg => lg.ChildGroups)
                .HasForeignKey(e => e.ParentGroupId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Location
        modelBuilder.Entity<Location>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.LocationGroupId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);

            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Locations)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.LocationGroup)
                .WithMany(lg => lg.Locations)
                .HasForeignKey(e => e.LocationGroupId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Manager)
                .WithMany()
                .HasForeignKey(e => e.ManagerUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // UserLocationAccess
        modelBuilder.Entity<UserLocationAccess>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.LocationId);
            entity.HasIndex(e => e.LocationGroupId);

            entity.HasOne(e => e.User)
                .WithMany(u => u.LocationAccesses)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Location)
                .WithMany(l => l.UserAccesses)
                .HasForeignKey(e => e.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.LocationGroup)
                .WithMany(lg => lg.UserAccesses)
                .HasForeignKey(e => e.LocationGroupId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Update User for Organization
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasOne(u => u.Organization)
                .WithMany(o => o.Users)
                .HasForeignKey(u => u.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Update Business for Organization
        modelBuilder.Entity<Business>(entity =>
        {
            entity.HasOne(b => b.Organization)
                .WithMany(o => o.Businesses)
                .HasForeignKey(b => b.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Update Review for Location
        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasIndex(e => e.LocationId);

            entity.HasOne(r => r.ReviewLocation)
                .WithMany(l => l.Reviews)
                .HasForeignKey(r => r.LocationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Update PlatformConnection for Location
        modelBuilder.Entity<PlatformConnection>(entity =>
        {
            entity.HasIndex(e => e.LocationId);

            entity.HasOne(pc => pc.PlatformLocation)
                .WithMany(l => l.PlatformConnections)
                .HasForeignKey(pc => pc.LocationId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
