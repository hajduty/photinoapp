using JobTracker.Application.Features.JobTracker;
using JobTracker.Application.Features.Notification;
using JobTracker.Application.Features.Postings;
using JobTracker.Application.Features.Settings;
using JobTracker.Application.Features.Tags;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public DbSet<Posting> Postings { get; set; } = null!;
    public DbSet<Features.JobTracker.JobTracker> JobTrackers { get; set; } = null!;
    public DbSet<Tag> Tags { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<Settings> Settings { get; set; } = null!;

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure many-to-many relationship between JobAlert and Tag
        modelBuilder.Entity<Features.JobTracker.JobTracker>()
            .HasMany(j => j.Tags)
            .WithMany(t => t.JobTrackers)
            .UsingEntity(j => j.ToTable("JobAlertTags"));
    }
}